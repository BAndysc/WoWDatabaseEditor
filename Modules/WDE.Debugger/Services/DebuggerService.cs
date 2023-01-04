using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Events;
using WDE.Common;
using WDE.Common.Debugging;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using Exception = System.Exception;

namespace WDE.Debugger.Services;

[AutoRegister]
[SingleInstance]
internal class DebuggerService : IDebuggerService
{
    private readonly IRemoteConnectorService remoteConnectorService;
    private readonly IUserSettings userSettings;
    private readonly IDebuggerDelayedSaveService delayedSaveService;
    private readonly IEventAggregator eventAggregator;

    internal class DebugPointModel
    {
        public readonly DebugPointId Id;
        public string? Log;
        public string? LogCondition;
        public bool Activated;
        public bool Enabled;
        public bool SuspendExecution;
        public bool GenerateStacktrace;
        public bool IsBreakpointHit;
        public BreakpointState State;
        public IDebugPointPayload Payload;
        public IDebugPointSource Source;

        public DebugPointModel(DebugPointId id, IDebugPointPayload payload, IDebugPointSource source)
        {
            Id = id;
            Payload = payload;
            Source = source;
        }
    }

    private List<DebugPointModel?> debugPoints = new();
    private IEnumerable<DebugPointModel> ActiveDebugPoints => debugPoints.Where(d => d != null).Select(d => d!);
    private List<DebugPointId> freeIds = new();
    private IReadOnlyList<IDebugPointSource> sources;
    private Dictionary<string, IDebugPointSource> sourcesByKey = new();

    public DebuggerService(IRemoteConnectorService remoteConnectorService,
        IUserSettings userSettings,
        IDebuggerDelayedSaveService delayedSaveService,
        IEventAggregator eventAggregator,
        IEnumerable<IDebugPointSource> sources)
    {
        this.remoteConnectorService = remoteConnectorService;
        this.userSettings = userSettings;
        this.delayedSaveService = delayedSaveService;
        this.eventAggregator = eventAggregator;
        this.sources = sources.ToList();
        sourcesByKey = this.sources.ToDictionary(s => s.Key);
        remoteConnectorService.EditorDisconnected += ServerDisconnected;
        remoteConnectorService.EditorConnected += ServerConnected;
        LoadDebugPoints();
        if (remoteConnectorService.IsConnected)
        {
            pendingResync = new();
            FetchExisting(pendingResync.Token, null).ListenErrors();
        }

        this.eventAggregator.GetEvent<IdeBreakpointHitEvent>()
            .Subscribe(OnIdeBreakpointHit);
        this.eventAggregator.GetEvent<IdeBreakpointResumeEvent>()
            .Subscribe(OnBreakpointResume);
    }

    private void OnIdeBreakpointHit(IdeBreakpointHitEventArgs args)
    {
        if (args.Line == null ||
            !args.Line.Contains("CHECK_BREAKPOINT"))
            return;

        foreach (var debugPoint in debugPoints)
        {
            if (debugPoint == null)
                continue;

            var source = debugPoint.Source;

            if (!source.IsBreakpointHit(args, debugPoint.Id, debugPoint.Payload))
                continue;

            debugPoint.IsBreakpointHit = true;
            args.HitDebugPoint = debugPoint.Id;
            CallDebugPointChangedInternal(debugPoint.Id);
            break;
        }
    }

    private void OnBreakpointResume()
    {
        foreach (var debugPoint in debugPoints)
        {
            if (debugPoint == null)
                continue;

            if (!debugPoint.IsBreakpointHit)
                continue;

            debugPoint.IsBreakpointHit = false;
            CallDebugPointChangedInternal(debugPoint.Id);
        }
    }

    internal class DebugPointSnapshot
    {
        [JsonConstructor]
        public DebugPointSnapshot()
        {

        }

        public DebugPointSnapshot(DebugPointModel model)
        {
            Log = model.Log;
            LogCondition = model.LogCondition;
            Activated = model.Activated;
            Enabled = model.Enabled;
            SuspendExecution = model.SuspendExecution;
            GenerateStacktrace = model.GenerateStacktrace;
            SourceName = model.Source.Key;
            Payload = model.Source.SerializePayload(model.Id);
        }

        public string? Log { get; set; }
        public string? LogCondition { get; set; }
        public bool Activated { get; set; }
        public bool Enabled { get; set; }
        public bool SuspendExecution { get; set; }
        public bool GenerateStacktrace { get; set; }
        public JObject? Payload { get; set; }
        public string SourceName { get; set; } = "";
    }

    internal class DebugPointsSnapshot : ISettings
    {
        public List<DebugPointSnapshot> Snapshots { get; set; } = new();
    }

    // internal only for tests to explicitly call it after initialization
    internal void LoadDebugPoints()
    {
        void LoadSnapshot(DebugPointSnapshot snapshot)
        {
            if (!sourcesByKey.TryGetValue(snapshot.SourceName, out var source))
            {
                LOG.LogWarning("Couldn't deserialize debug point with key: " + snapshot.SourceName);
                return;
            }

            if (snapshot.Payload == null)
                throw new Exception("Payload is null");

            var id = AllocId();
            var payload = source.DeserializePayload(snapshot.Payload);
            debugPoints[id.Index] = new DebugPointModel(id, payload, source)
            {
                Log = snapshot.Log,
                LogCondition = snapshot.LogCondition,
                Activated = snapshot.Activated,
                Enabled = snapshot.Enabled,
                SuspendExecution = snapshot.SuspendExecution,
                GenerateStacktrace = snapshot.GenerateStacktrace,
                State = BreakpointState.Pending
            };
        }

        try
        {
            var snapshots = userSettings.Get<DebugPointsSnapshot>();
            if (snapshots == null)
                return;

            foreach (var snapshot in snapshots.Snapshots)
            {
                try
                {
                    LoadSnapshot(snapshot);
                }
                catch (Exception e)
                {
                    LOG.LogError(e, message: "Couldn't load a snapshot");
                }
            }
        }
        catch (Exception e)
        {
            LOG.LogError(e, message: "Couldn't load snapshots");
        }
    }

    private void ScheduleSaveDebugPoints()
    {
        delayedSaveService.ScheduleSave(DoSaveInternal);
    }

    private void DoSaveInternal()
    {
        var snapshot = ActiveDebugPoints
            .Select(x => new DebugPointSnapshot(x))
            .ToList();

        userSettings.Update(new DebugPointsSnapshot()
        {
            Snapshots = snapshot
        });
    }

    private CancellationTokenSource? pendingResync;

    private async Task FetchExisting(CancellationToken token, List<Exception>? exceptions)
    {
        foreach (var source in Sources)
        {
            try
            {
                await source.FetchFromServerAsync(token);
            }
            catch (Exception e)
            {
                exceptions?.Add(e);
            }
            if (token.IsCancellationRequested)
                return;
        }
    }

    private async Task SynchronizePointAndUpdateState(DebugPointModel debug)
    {
        try
        {
            if (!remoteConnectorService.IsConnected)
            {
                debug.State = BreakpointState.Pending;
                return;
            }
            var result = await debug.Source.Synchronizer.Synchronize(debug.Id);
            if (result == SynchronizationResult.Ok)
                debug.State = BreakpointState.Synced;
            else if (result == SynchronizationResult.OutOfSync)
                debug.State = BreakpointState.WaitingForSync;
            else
                throw new ArgumentOutOfRangeException(nameof(result));
        }
        catch (OperationCanceledException)
        {
            debug.State = BreakpointState.Pending;
        }
        catch (Exception)
        {
            debug.State = BreakpointState.SynchronizationError;
            throw;
        }
        finally
        {
            CallDebugPointChangedInternal(debug.Id);
        }
    }

    private void ServerConnected()
    {
        async Task SynchronizeAsync(CancellationToken token)
        {
            List<Exception>? errors = null;
            await FetchExisting(token, errors);
            foreach (var debug in ActiveDebugPoints)
            {
                try
                {
                    if (!debug.Enabled || !debug.Activated)
                        continue;

                    // was just fetched
                    if (debug.State == BreakpointState.Synced)
                        continue;

                    await SynchronizePointAndUpdateState(debug);

                    if (token.IsCancellationRequested)
                        return;
                }
                catch (Exception e)
                {
                    errors ??= new();
                    errors.Add(e);
                }
            }
            if (errors != null)
                throw new AggregateException(errors);
        }
        pendingResync?.Cancel();
        pendingResync = new CancellationTokenSource();
        SynchronizeAsync(pendingResync.Token).ListenErrors();
    }

    private void ServerDisconnected()
    {
        foreach (var debug in ActiveDebugPoints)
        {
            debug.State = BreakpointState.Pending;
            CallDebugPointChangedInternal(debug.Id);
        }
    }

    private DebugPointId AllocId()
    {
        if (freeIds.Count > 0)
        {
            var id = freeIds[^1];
            freeIds.RemoveAt(freeIds.Count - 1);
            return new DebugPointId(id.Index, id.Version + 1);
        }
        else
        {
            debugPoints.Add(null!);
            return new DebugPointId(debugPoints.Count - 1, 0);
        }
    }

    public DebugPointId CreateDebugPoint(IDebugPointSource source, IDebugPointPayload payload)
    {
        var id = AllocId();
        debugPoints[id.Index] = new DebugPointModel(id, payload, source)
        {
            Enabled = true,
            Activated = true,
            SuspendExecution = false,
            GenerateStacktrace = true,
            State = BreakpointState.Pending
        };
        DebugPointAdded?.Invoke(id);
        ScheduleSaveDebugPoints();
        return id;
    }

    private void CallDebugPointChangedInternal(DebugPointId id)
    {
        ScheduleSaveDebugPoints();
        DebugPointChanged?.Invoke(id);
    }
    
    private void RemoveDebugPointInternal(DebugPointId id)
    {
        if (!ThrowIfInvalidId(id))
            return;

        DebugPointRemoving?.Invoke(id);
        debugPoints[id.Index] = null!;
        freeIds.Add(id);
        DebugPointRemoved?.Invoke(id);
        ScheduleSaveDebugPoints();
    }

    public void ScheduleRemoveDebugPoint(DebugPointId id)
    {
        if (!ThrowIfInvalidId(id))
            return;

        debugPoints[id.Index]!.State = BreakpointState.PendingRemoval;
        CallDebugPointChangedInternal(id);
    }

    public async Task RemoveDebugPointAsync(DebugPointId id, bool remoteAlreadyRemoved)
    {
        if (remoteAlreadyRemoved)
        {
            RemoveDebugPointInternal(id);
        }
        else
        {
            ScheduleRemoveDebugPoint(id);
            await Synchronize(id);
        }
    }

    public async Task ClearAllDebugPoints()
    {
        List<IDebugPointSource> sourcesToClear = new();
        List<Exception>? exceptions = null;
        foreach (var source in sources)
        {
            try
            {
                if (remoteConnectorService.IsConnected)
                    await source.ForceClearAllAsync();

                sourcesToClear.Add(source);
            }
            catch (Exception e)
            {
                exceptions ??= new();
                exceptions.Add(e);
            }
        }
        for (int i = debugPoints.Count - 1; i >= 0; --i)
        {
            if (debugPoints[i] == null)
                continue;

            var debugPoint = debugPoints[i]!;
            RemoveDebugPointInternal(debugPoint.Id);
        }

        if (exceptions != null)
            throw new AggregateException(exceptions);
    }

    public async Task RemoveIfAsync<T>(Func<T, bool> matcher, bool remoteAlreadyRemoved) where T : IDebugPointPayload
    {
        for (int i = debugPoints.Count - 1; i >= 0; --i)
        {
            if (debugPoints[i] == null)
                continue;

            if (debugPoints[i]!.Payload is T p && matcher(p))
                await RemoveDebugPointAsync(debugPoints[i]!.Id, remoteAlreadyRemoved);
        }
    }

    private bool ThrowIfInvalidId(DebugPointId id)
    {
        if (id.Index < 0 || id.Index >= debugPoints.Count || debugPoints[id.Index] == null || debugPoints[id.Index]!.Id != id)
        {
#if DEBUG
            throw new InvalidOperationException("Invalid debug point id");
#else
            return false;
#endif
        }
        return true;
    }

    public bool TryFindDebugPoint<T>(Func<T, bool> matcher, out DebugPointId id) where T : IDebugPointPayload
    {
        for (int i = 0; i < debugPoints.Count; ++i)
        {
            if (debugPoints[i] == null)
                continue;

            if (debugPoints[i]!.Payload is T p && matcher(p))
            {
                id = debugPoints[i]!.Id;
                return true;
            }
        }

        id = default!;
        return false;
    }

    public bool TryGetPayload<T>(DebugPointId id, out T payload) where T : IDebugPointPayload
    {
        if (!ThrowIfInvalidId(id))
        {
            payload = default!;
            return false;
        }

        if (debugPoints[id.Index]!.Payload is T p)
        {
            payload = p;
            return true;
        }

        payload = default!;
        return false;
    }

    public void SetLog(DebugPointId id, string? path)
    {
        if (!ThrowIfInvalidId(id))
            return;

        var debug = debugPoints[id.Index]!;
        debug.Log = path;
        if (debug.State == BreakpointState.Synced)
            debug.State = BreakpointState.Pending;
        CallDebugPointChangedInternal(id);
    }

    public void SetEnabled(DebugPointId id, bool enabled)
    {
        if (!ThrowIfInvalidId(id))
            return;

        var debug = debugPoints[id.Index]!;
        debug.Enabled = enabled;
        if (debug.State == BreakpointState.Synced)
            debug.State = BreakpointState.Pending;
        CallDebugPointChangedInternal(id);
    }

    public void SetActivated(DebugPointId point, bool activated)
    {
        if (!ThrowIfInvalidId(point))
            return;

        var debug = debugPoints[point.Index]!;
        debug.Activated = activated;
        if (debug.State is BreakpointState.Synced)
            debug.State = BreakpointState.Pending;
        CallDebugPointChangedInternal(point);
    }

    public void SetSuspendExecution(DebugPointId id, bool suspend)
    {
        if (!ThrowIfInvalidId(id))
            return;

        var debug = debugPoints[id.Index]!;
        debug.SuspendExecution = suspend;
        if (debug.State == BreakpointState.Synced)
            debug.State = BreakpointState.Pending;
        CallDebugPointChangedInternal(id);
    }

    public void SetGenerateStacktrace(DebugPointId id, bool generate)
    {
        if (!ThrowIfInvalidId(id))
            return;

        var debug = debugPoints[id.Index]!;
        debug.GenerateStacktrace = generate;
        if (debug.State == BreakpointState.Synced)
            debug.State = BreakpointState.Pending;
        CallDebugPointChangedInternal(id);
    }

    public void SetPayload(DebugPointId id, IDebugPointPayload payload)
    {
        if (!ThrowIfInvalidId(id))
            return;

        var debug = debugPoints[id.Index]!;
        debug.Payload = payload;
        if (debug.State == BreakpointState.Synced)
            debug.State = BreakpointState.Pending;
        CallDebugPointChangedInternal(id);
    }

    public void SetLogCondition(DebugPointId id, string? condition)
    {
        if (!ThrowIfInvalidId(id))
            return;

        var debug = debugPoints[id.Index]!;
        debug.LogCondition = condition;
        CallDebugPointChangedInternal(id);
    }

    public void ForceSetSynchronized(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return;

        if (!IsConnected)
            return;

        debugPoints[point.Index]!.State = BreakpointState.Synced;
        CallDebugPointChangedInternal(point);
    }

    public void ForceSetPending(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return;

        debugPoints[point.Index]!.State = BreakpointState.Pending;
        CallDebugPointChangedInternal(point);
    }

    public string? GetLogFormat(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return null;

        return debugPoints[point.Index]!.Log;
    }

    public string? GetLogCondition(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return null;

        return debugPoints[point.Index]!.LogCondition;
    }

    public bool GetActivated(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return false;

        return debugPoints[point.Index]!.Activated;
    }

    public bool GetEnabled(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return false;

        return debugPoints[point.Index]!.Enabled;
    }

    public bool GetSuspendExecution(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return false;

        return debugPoints[point.Index]!.SuspendExecution;
    }

    public bool GetGenerateStacktrace(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return false;

        return debugPoints[point.Index]!.GenerateStacktrace;
    }

    public bool IsBreakpointHit(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return false;

        return debugPoints[point.Index]!.IsBreakpointHit;
    }

    public IDebugPointSource GetSource(DebugPointId point)
    {
        if (!ThrowIfInvalidId(point))
            return null!;

        return debugPoints[point.Index]!.Source;
    }

    public bool HasDebugPoint(DebugPointId id)
    {
        return id.Index >= 0 && id.Index < debugPoints.Count && debugPoints[id.Index] != null && debugPoints[id.Index]!.Id == id;
    }

    public async Task Synchronize(DebugPointId id)
    {
        if (!ThrowIfInvalidId(id))
            return;

        var debug = debugPoints[id.Index]!;
        if (debug.State == BreakpointState.Pending)
        {
            if (debug.Enabled && debug.Activated)
                await SynchronizePointAndUpdateState(debug);
            else
            {
                try
                {
                    await debug.Source.Synchronizer.Delete(id);
                    debug.State = BreakpointState.Synced;
                }
                catch (OperationCanceledException)
                {
                    debug.State = BreakpointState.Pending;
                }
                catch (Exception)
                {
                    debug.State = BreakpointState.SynchronizationError;
                    throw;
                }
                finally
                {
                    CallDebugPointChangedInternal(id);
                }
            }
        }
        else if (debug.State == BreakpointState.PendingRemoval)
        {
            try
            {
                if (remoteConnectorService.IsConnected)
                    await debug.Source.Synchronizer.Delete(id);
                RemoveDebugPointInternal(id);
            }
            catch (DebuggingFeaturesDisabledException)
            {
                RemoveDebugPointInternal(id);
                throw;
            }
            catch (Exception)
            {
                debug.State = BreakpointState.SynchronizationError;
                CallDebugPointChangedInternal(id);
                throw;
            }
        }
    }

    public BreakpointState GetState(DebugPointId breakpoint)
    {
        if (!ThrowIfInvalidId(breakpoint))
            return BreakpointState.PendingRemoval;

        return debugPoints[breakpoint.Index]!.State;
    }

    public bool IsConnected => remoteConnectorService.IsConnected;

    public IReadOnlyList<IDebugPointSource> Sources => sources;
    public IEnumerable<DebugPointId> DebugPoints => ActiveDebugPoints.Select(d => d.Id);

    public event Action<DebugPointId>? DebugPointAdded;
    public event Action<DebugPointId>? DebugPointRemoving;
    public event Action<DebugPointId>? DebugPointRemoved;
    public event Action<DebugPointId>? DebugPointChanged;
}