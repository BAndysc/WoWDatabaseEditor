using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.AnalyticsUsage;

[AutoRegister]
[SingleInstance]
public class UsageManager
{
    private readonly UsageConfigService configService;
    private readonly AptabaseAnalyticsService analyticsService;
    private readonly DocumentUsageService documentTracker;
    private readonly IMessageBoxService messageBoxService;
    private readonly ILoadingEventAggregator loadingEventAggregator;
    private readonly IMainThread mainThread;
    private readonly ICurrentCoreVersion currentCoreVersion;
    private readonly IEnumerable<IAnalyticsSessionPropsProvider> sessionPropsProviders;

    public UsageManager(UsageConfigService configService,
        AptabaseAnalyticsService analyticsService,
        DocumentUsageService documentTracker,
        IMessageBoxService messageBoxService,
        ILoadingEventAggregator loadingEventAggregator,
        IMainThread mainThread,
        ICurrentCoreVersion currentCoreVersion,
        IEnumerable<IAnalyticsSessionPropsProvider> sessionPropsProviders)
    {
        this.configService = configService;
        this.analyticsService = analyticsService;
        this.documentTracker = documentTracker;
        this.messageBoxService = messageBoxService;
        this.loadingEventAggregator = loadingEventAggregator;
        this.mainThread = mainThread;
        this.currentCoreVersion = currentCoreVersion;
        this.sessionPropsProviders = sessionPropsProviders;

        if (OperatingSystem.IsBrowser())
            return;

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Initialize().ListenErrors();
    }

    // like ReportErrorsSinkManager, this must not be an IGlobalAsyncInitializer,
    // because the message box service is not initialized yet at that point
    private async Task Initialize()
    {
        // measured before the consent dialog can delay us: time from the process start
        // to the editor being fully loaded (UsageManager is resolved right after EditorLoaded)
        var startupMs = (int)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;

        if (!configService.Enabled.HasValue)
        {
            // the consent dialog needs the main window to exist and be activated,
            // otherwise the message box service has no owner window and the dialog
            // silently never shows, so wait for the editor and then some
            await WaitForEditorLoaded();
            await Delay(TimeSpan.FromSeconds(1));

            for (int attempt = 0; attempt < 5 && !configService.Enabled.HasValue; ++attempt)
            {
                try
                {
                    await AskUsersForConsent();
                }
                catch (Exception e)
                {
                    LOG.LogWarning(e, "Couldn't show the usage consent dialog, retrying");
                    await Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        var props = new Dictionary<string, object>()
        {
            ["core"] = currentCoreVersion.IsSpecified ? currentCoreVersion.Current.Tag : "unspecified",
            ["startup_ms"] = startupMs
        };
        foreach (var provider in sessionPropsProviders)
        {
            try
            {
                foreach (var (key, value) in provider.GetSessionProps())
                    props[key] = value;
            }
            catch (Exception e)
            {
                LOG.LogWarning(e, "Usage session props provider {0} failed", provider.GetType().Name);
            }
        }
        analyticsService.TrackEvent("app_started", props.Select(p => (p.Key, p.Value)).ToArray());
    }

    private Task WaitForEditorLoaded()
    {
        var completionSource = new TaskCompletionSource();
        loadingEventAggregator.OnEvent<EditorLoaded>().SubscribeOnce(_ => completionSource.TrySetResult());
        return completionSource.Task;
    }

    private Task Delay(TimeSpan delay)
    {
        var completionSource = new TaskCompletionSource();
        mainThread.Delay(() => completionSource.TrySetResult(), delay);
        return completionSource.Task;
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        // order matters: first the tracker enqueues its final aggregates,
        // then the service persists everything that wasn't sent yet
        documentTracker.FlushOnExit();
        analyticsService.PersistPendingOnExit();
    }

    private async Task AskUsersForConsent()
    {
        configService.Enabled = await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Anonymous usage statistics")
            .SetMainInstruction("Do you want to enable anonymous usage statistics?")
            .SetContent(
                "If enabled, the editor will send anonymous usage statistics: which editors and tools are used and how much time is spent in them, together with a random installation identifier. This helps the developers to know which features matter.\n\nNo database content, no queries, no file paths nor any personal data is ever sent.\n\nYou can always change this setting in the settings menu.")
            .WithYesButton(true)
            .WithNoButton(false)
            .Build());
    }
}
