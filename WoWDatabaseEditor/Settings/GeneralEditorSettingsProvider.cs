using System;
using System.ComponentModel;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Settings;

[AutoRegisterToParentScope]
[SingleInstance]
public class GeneralEditorSettingsProvider : IGeneralEditorSettingsProvider
{
    public struct Data : ISettings
    {
        [DefaultValue(RestoreOpenTabsMode.RestoreWhenCrashed)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public RestoreOpenTabsMode RestoreOpenTabsMode;
    }

    private Data currentData;
    
    private readonly IUserSettings userSettings;

    public GeneralEditorSettingsProvider(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        currentData = userSettings.Get<Data>(new Data(){RestoreOpenTabsMode = RestoreOpenTabsMode.RestoreWhenCrashed});
    }

    private void Save()
    {
        userSettings.Update(currentData);
    }
    
    public RestoreOpenTabsMode RestoreOpenTabsMode
    {
        get => currentData.RestoreOpenTabsMode;
        set => currentData.RestoreOpenTabsMode = value;
    }

    public void Apply()
    {
        Save();
    }
}