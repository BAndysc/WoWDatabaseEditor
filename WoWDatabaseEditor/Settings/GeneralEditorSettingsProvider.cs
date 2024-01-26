using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PropertyChanged.SourceGenerator;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Settings;

[AutoRegister]
[SingleInstance]
/*
 * This is a temporary class until Singleton and NonUnique registrations are properly supported.
 * Once they are, this can be merged to GeneralEditorSettingsProvider below. Basically some another class needs to observer
 * ToolBarButtonStyle changes, but they can't reference IGeneralEditorSettingsProvider now, because this will create another copy
 * of the IGeneralEditorSettingsProvider.
 */
public partial class TempToolbarButtonStyleService
{
    [Notify] private ToolBarButtonStyle style;
}

[AutoRegisterToParentScope]
[SingleInstance]
public class GeneralEditorSettingsProvider : IGeneralEditorSettingsProvider
{
    public struct Data : ISettings
    {
        [DefaultValue(RestoreOpenTabsMode.RestoreWhenCrashed)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [JsonConverter(typeof(StringEnumConverter))]
        public RestoreOpenTabsMode RestoreOpenTabsMode;

        [DefaultValue(ToolBarButtonStyle.Icon)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ToolBarButtonStyle ToolBarButtonStyle;
    }

    private Data currentData;
    
    private readonly IUserSettings userSettings;
    private readonly TempToolbarButtonStyleService tempToolbarButtonStyleService;

    public GeneralEditorSettingsProvider(IUserSettings userSettings,
        TempToolbarButtonStyleService tempToolbarButtonStyleService)
    {
        this.userSettings = userSettings;
        this.tempToolbarButtonStyleService = tempToolbarButtonStyleService;
        currentData = userSettings.Get<Data>(new Data()
        {
            RestoreOpenTabsMode = RestoreOpenTabsMode.RestoreWhenCrashed,
            ToolBarButtonStyle = ToolBarButtonStyle.Icon
        });
        tempToolbarButtonStyleService.Style = currentData.ToolBarButtonStyle;
    }

    private void Save()
    {
        tempToolbarButtonStyleService.Style = currentData.ToolBarButtonStyle;
        userSettings.Update(currentData);
    }
    
    public RestoreOpenTabsMode RestoreOpenTabsMode
    {
        get => currentData.RestoreOpenTabsMode;
        set => currentData.RestoreOpenTabsMode = value;
    }
    
    public ToolBarButtonStyle ToolBarButtonStyle
    {
        get => currentData.ToolBarButtonStyle;
        set => currentData.ToolBarButtonStyle = value;
    }

    public void Apply()
    {
        Save();
    }
}