using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using WDE.Common;
using WDE.Common.Settings;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.LogService.ReportErrorsToServer;

[AutoRegister]
[SingleInstance]
public class ReportErrorsConfigurationViewModel : ObservableBase, IGeneralSettingsGroup
{
    private readonly IReportErrorsConfigService service;
    public string Name => "Automatic errors reporting";

    public IReadOnlyList<IGenericSetting> Settings { get; set; }

    private BoolGenericSetting allowErrorsReporting;

    public ReportErrorsConfigurationViewModel(IReportErrorsConfigService service)
    {
        this.service = service;
        allowErrorsReporting = new BoolGenericSetting("Automatically send error reports", service.AllowErrorsReporting ?? false, "If enabled, the application will automatically send error reports to the developers. This helps us to improve the application and fix bugs faster.");
        Settings = new List<IGenericSetting>() { allowErrorsReporting };
    }

    public void Save()
    {
        service.AllowErrorsReporting = allowErrorsReporting.Value;
    }
}