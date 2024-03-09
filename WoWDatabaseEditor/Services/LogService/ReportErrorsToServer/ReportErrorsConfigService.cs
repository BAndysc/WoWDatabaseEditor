using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.LogService.ReportErrorsToServer;

[SingleInstance]
[AutoRegister]
public class ReportErrorsConfigService : IReportErrorsConfigService
{
    private readonly IUserSettings userSettings;

    private bool? allowErrorsReporting;
    public bool? AllowErrorsReporting
    {
        get => allowErrorsReporting;
        set
        {
            allowErrorsReporting = value;
            userSettings.Update(new Config()
            {
                AllowErrorsReporting = allowErrorsReporting
            });
        }
    }

    public ReportErrorsConfigService(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        var settings = userSettings.Get<Config>();
        AllowErrorsReporting = settings.AllowErrorsReporting;
    }

    public struct Config : ISettings
    {
        public bool? AllowErrorsReporting { get; set; }
    }
}