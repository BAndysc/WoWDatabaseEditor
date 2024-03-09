using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.LogService.ReportErrorsToServer;

[UniqueProvider]
public interface IReportErrorsConfigService
{
    bool? AllowErrorsReporting { get; set; }
}