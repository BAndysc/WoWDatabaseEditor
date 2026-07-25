using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WDE.Common;
using WDE.Common.Factories;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.AnalyticsUsage;

/// <summary>
/// Sends anonymous usage events to Aptabase (https://aptabase.com).
/// Events are queued in memory, flushed in batches in the background and
/// never block nor crash the editor. Events that couldn't be sent (offline)
/// are persisted on exit and retried on the next run.
/// There is no user nor device identifier, only a random per-run session id.
/// </summary>
[AutoRegister]
[SingleInstance]
public class AptabaseAnalyticsService : IAnalyticsService, IDisposable
{
    private const string AppKey = "A-EU-8251754029";
    private const string ApiUrl = "https://eu.aptabase.com/api/v0/events";
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CountersFlushInterval = TimeSpan.FromMinutes(10);
    private const int MaxQueuedEvents = 1000;
    private const int MaxCounters = 500;
    private const int MaxEventsPerRequest = 25;

#if DEBUG
    private const bool IsDebugBuild = true;
#else
    private const bool IsDebugBuild = false;
#endif

    private readonly IUserSettings userSettings;
    private readonly UsageConfigService configService;
    private readonly IHttpClientFactory httpClientFactory;

    private readonly object queueLock = new();
    private readonly List<QueuedEvent> queue = new();
    private readonly Dictionary<string, CounterEntry> counters = new();
    private DateTime lastCountersFlush = DateTime.UtcNow;
    private readonly SemaphoreSlim sendLock = new(1);
    private readonly string sessionId;
    private readonly string installId;
    private readonly JObject systemProps;
    private readonly IDisposable flushTimer;
    private HttpClient? client;

    public bool Enabled => configService.Enabled ?? false;

    public AptabaseAnalyticsService(IUserSettings userSettings,
        UsageConfigService configService,
        IApplicationVersion applicationVersion,
        IApplicationReleaseConfiguration applicationReleaseConfiguration,
        IHttpClientFactory httpClientFactory,
        IMainThread mainThread)
    {
        this.userSettings = userSettings;
        this.configService = configService;
        this.httpClientFactory = httpClientFactory;

        sessionId = NewSessionId();
        // the same installation identifier the updater uses (unique per downloaded copy),
        // hashed so the raw update key never leaves the user's machine;
        // self-compiled builds have no update key, so they get a random persistent guid instead
        var updateKey = applicationReleaseConfiguration.GetString("UPDATE_KEY");
        installId = string.IsNullOrEmpty(updateKey)
            ? configService.InstallId.ToString("N")
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(updateKey)))[..16].ToLowerInvariant();
        var version = applicationVersion.VersionKnown ? applicationVersion.BuildVersion.ToString() : "dev";
        systemProps = new JObject()
        {
            ["isDebug"] = IsDebugBuild,
            ["locale"] = CultureInfo.CurrentCulture.Name,
            ["osName"] = OperatingSystem.IsWindows() ? "Windows"
                : OperatingSystem.IsMacOS() ? "macOS"
                : OperatingSystem.IsLinux() ? "Linux"
                : OperatingSystem.IsBrowser() ? "Browser"
                : Environment.OSVersion.Platform.ToString(),
            ["osVersion"] = Environment.OSVersion.Version.ToString(),
            ["appVersion"] = version,
            ["appBuildNumber"] = version,
            ["sdkVersion"] = "wde-aptabase@1.0.0"
        };

        var pending = userSettings.Get<PendingUsageEvents>();
        if (pending.Events is { Count: > 0 })
        {
            queue.AddRange(pending.Events.Take(MaxQueuedEvents));
            userSettings.Update(new PendingUsageEvents());
        }

        configService.EnabledChanged += OnEnabledChanged;
        flushTimer = mainThread.StartTimer(OnFlushTick, FlushInterval);
        USAGE.Service = this;
    }

    public void Dispose()
    {
        if (ReferenceEquals(USAGE.Service, this))
            USAGE.Service = null;
        flushTimer.Dispose();
        configService.EnabledChanged -= OnEnabledChanged;
        client?.Dispose();
    }

    public void TrackEvent(string eventName, params (string key, object value)[] props)
    {
        if (!Enabled)
            return;

        var @event = new QueuedEvent()
        {
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Name = eventName,
            Props = props.Length == 0 ? null : props.ToDictionary(p => p.key, p => p.value)
        };
        lock (queueLock)
        {
            if (queue.Count < MaxQueuedEvents)
                queue.Add(@event);
        }
    }

    public void CountEvent(string eventName, params (string key, object value)[] groupProps)
        => CountEvent(eventName, 1, groupProps);

    public void CountEvent(string eventName, double amount, params (string key, object value)[] groupProps)
    {
        if (!Enabled)
            return;

        // counters are grouped by the wall-clock window the increment happened in,
        // and the flushed event is timestamped with that window - this way
        // time-bucketed aggregations are exact regardless of when the flush happens
        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Ticks - now.Ticks % CountersFlushInterval.Ticks, DateTimeKind.Utc);

        var groupKey = windowStart.Ticks + "\n" + eventName;
        foreach (var prop in groupProps)
            groupKey += "\n" + prop.key + "\n" + prop.value;

        lock (queueLock)
        {
            if (counters.TryGetValue(groupKey, out var entry))
                entry.Amount += amount;
            else if (counters.Count < MaxCounters)
                counters[groupKey] = new CounterEntry(eventName, groupProps, windowStart, amount);
        }
    }

    /// <summary>
    /// Called on process exit (by <see cref="UsageManager"/>) so that events
    /// which weren't sent yet are not lost, but sent on the next run.
    /// </summary>
    internal void PersistPendingOnExit()
    {
        if (!Enabled)
            return;

        FlushCounters();
        lock (queueLock)
        {
            if (queue.Count > 0)
                userSettings.Update(new PendingUsageEvents() { Events = queue.ToList() });
        }
    }

    private void OnEnabledChanged()
    {
        if (Enabled)
            return;

        lock (queueLock)
        {
            queue.Clear();
            counters.Clear();
        }
    }

    private bool OnFlushTick()
    {
        if (DateTime.UtcNow - lastCountersFlush >= CountersFlushInterval)
            FlushCounters();
        FlushAsync().ListenErrors();
        return true;
    }

    private void FlushCounters()
    {
        lock (queueLock)
        {
            lastCountersFlush = DateTime.UtcNow;
            foreach (var counter in counters.Values)
            {
                if (queue.Count >= MaxQueuedEvents)
                    break;

                var count = (long)Math.Round(counter.Amount);
                if (count == 0)
                    continue;

                var props = counter.GroupProps.ToDictionary(p => p.key, p => p.value);
                props["count"] = count;
                queue.Add(new QueuedEvent()
                {
                    Timestamp = counter.WindowStart,
                    SessionId = sessionId,
                    Name = counter.EventName,
                    Props = props
                });
            }
            counters.Clear();
        }
    }

    private async Task FlushAsync()
    {
        if (!await sendLock.WaitAsync(0))
            return;

        try
        {
            while (Enabled)
            {
                QueuedEvent[] batch;
                lock (queueLock)
                    batch = queue.Take(MaxEventsPerRequest).ToArray();

                if (batch.Length == 0)
                    break;

                var payload = new JArray(batch.Select(ToWireFormat).Cast<object>().ToArray());
                client ??= CreateClient();
                using var content = new StringContent(payload.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(ApiUrl, content);

                if ((int)response.StatusCode >= 500)
                    break; // server issue, keep the events and retry on the next tick

                if (!response.IsSuccessStatusCode)
                    LOG.LogWarning("Usage server rejected events: {0}", response.StatusCode);

                lock (queueLock)
                    queue.RemoveRange(0, Math.Min(batch.Length, queue.Count));
            }
        }
        catch (Exception)
        {
            // most likely no internet connection - the events stay queued and are retried later
        }
        finally
        {
            sendLock.Release();
        }
    }

    private HttpClient CreateClient()
    {
        var httpClient = httpClientFactory.Factory();
        httpClient.DefaultRequestHeaders.Add("App-Key", AppKey);
        return httpClient;
    }

    private JObject ToWireFormat(QueuedEvent @event)
    {
        var props = @event.Props != null ? JObject.FromObject(@event.Props) : new JObject();
        props["install_id"] = installId;
        return new JObject()
        {
            ["timestamp"] = @event.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            ["sessionId"] = @event.SessionId,
            ["eventName"] = @event.Name,
            ["systemProps"] = systemProps.DeepClone(),
            ["props"] = props
        };
    }

    // the session id format used by official Aptabase SDKs
    private static string NewSessionId()
    {
        var epochInSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = Random.Shared.Next(0, 100000000);
        return (epochInSeconds * 100000000L + random).ToString();
    }
}

public class CounterEntry
{
    public readonly string EventName;
    public readonly (string key, object value)[] GroupProps;
    public readonly DateTime WindowStart;
    public double Amount;

    public CounterEntry(string eventName, (string key, object value)[] groupProps, DateTime windowStart, double amount)
    {
        EventName = eventName;
        GroupProps = groupProps;
        WindowStart = windowStart;
        Amount = amount;
    }
}

public class QueuedEvent
{
    public DateTime Timestamp { get; set; }
    public string SessionId { get; set; } = "";
    public string Name { get; set; } = "";
    public Dictionary<string, object>? Props { get; set; }
}

public struct PendingUsageEvents : ISettings
{
    public List<QueuedEvent>? Events { get; set; }
}
