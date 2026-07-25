using System.Collections.Generic;
using WDE.Common.Settings;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.AnalyticsUsage;

[AutoRegister]
[SingleInstance]
public class UsageConfigurationViewModel : ObservableBase, IGeneralSettingsGroup
{
    private readonly UsageConfigService configService;
    private readonly BoolGenericSetting sendUsageStatistics;

    public string Name => "Anonymous usage statistics";

    public IReadOnlyList<IGenericSetting> Settings { get; }

    public UsageConfigurationViewModel(UsageConfigService configService)
    {
        this.configService = configService;
        sendUsageStatistics = new BoolGenericSetting("Send anonymous usage statistics",
            configService.Enabled ?? false,
            "If enabled, the editor sends anonymous usage statistics: which editors and tools are opened and how much time is spent in them, together with a random installation identifier. No database content, queries, file paths nor any personal data is ever sent.");
        Settings = new List<IGenericSetting>() { sendUsageStatistics };
    }

    public void Save()
    {
        configService.Enabled = sendUsageStatistics.Value;
    }
}
