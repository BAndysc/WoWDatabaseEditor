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
        // fixing incorrect data:
        // due to a little bug in saving, the values used to be negative, but that yields problem
        // after a new value was added, so need to fix that
        // the fix was added along with new enum value 256, before the fix 128 was the last enum value
        // thus if the value is negative, then we know we have to add 256 to it
        if (skipSources < 0)
            skipSources += 256;
    }
    
    public struct Data : ISettings
    {
        [DefaultValue(FindAnywhereSourceType.Spawns)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public FindAnywhereSourceType Skip;
    }
}