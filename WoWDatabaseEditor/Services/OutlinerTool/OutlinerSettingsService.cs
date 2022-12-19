using System;
using System.ComponentModel;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Common.Services.FindAnywhere;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.OutlinerTool;

[AutoRegister]
[SingleInstance]
public class OutlinerSettingsService : IOutlinerSettingsService
{
    private readonly IUserSettings userSettings;
    private FindAnywhereSourceType skipSources;

    public FindAnywhereSourceType SkipSources
    {
        get => skipSources;
        set
        {
            skipSources = value;
            userSettings.Update(new Data(){Skip = value});
            OnSettingsChanged?.Invoke();
        }
    }

    public event Action? OnSettingsChanged;

    public OutlinerSettingsService(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        skipSources = userSettings.Get<Data>().Skip;
    }
    
    public struct Data : ISettings
    {
        [DefaultValue(FindAnywhereSourceType.Spawns)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public FindAnywhereSourceType Skip;
    }
}