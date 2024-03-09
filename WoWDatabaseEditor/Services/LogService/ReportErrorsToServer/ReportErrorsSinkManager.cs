using System;
using System.Threading.Tasks;
using WDE.Common.Factories;
using WDE.Common.Modules;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.LogService.ReportErrorsToServer;

[AutoRegister]
[SingleInstance]
public class ReportErrorsSinkManager
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IApplicationReleaseConfiguration configuration;
    private readonly IReportErrorsConfigService configService;
    private readonly IMessageBoxService messageBoxService;
    private readonly ReportErrorsSink sink;

    public ReportErrorsSinkManager(IHttpClientFactory httpClientFactory,
        IApplicationReleaseConfiguration configuration,
        IReportErrorsConfigService configService,
        IMessageBoxService messageBoxService,
        ReportErrorsSink sink)
    {
        this.httpClientFactory = httpClientFactory;
        this.configuration = configuration;
        this.configService = configService;
        this.messageBoxService = messageBoxService;
        this.sink = sink;
        Initialize().ListenErrors();
    }

    // don't make this class implement IGlobalAsyncInitialize, because this is called before message box service is initialized
    // but we need message box service to ask user for consent otherwise it would be a deadlock
    private async Task Initialize()
    {
        if (OperatingSystem.IsBrowser())
        {
            sink.CloseSink();
        }
        else
        {
            if (!configService.AllowErrorsReporting.HasValue)
                await AskUsersForConsent();

            if (configService.AllowErrorsReporting ?? false)
            {
                sink.OpenSink(configuration, httpClientFactory);
            }
            else
                sink.CloseSink();
        }
    }

    private async Task AskUsersForConsent()
    {
        configService.AllowErrorsReporting = await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Error reporting")
            .SetMainInstruction("Do you want to enable automatic error reporting?")
            .SetContent(
                "If enabled, the application will automatically send error reports to the developers. This helps us to improve the application and fix bugs faster.\n\nYou can always change this setting in the settings menu.")
            .WithYesButton(true)
            .WithNoButton(false)
            .Build());
    }
}