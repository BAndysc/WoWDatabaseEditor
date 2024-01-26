using System.Collections.Generic;
using WDE.Common.Settings;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Settings;

[AutoRegister]
public class SettingsProvider : IGeneralSettingsGroup
{
    private readonly IGeneralEditorSettingsProvider editorSettingsProvider;
    public string Name => "General";
    public IReadOnlyList<IGenericSetting> Settings { get; }
    
    private ListOptionGenericSetting restoreOpenTabsMode;
    private ListOptionGenericSetting toolbarIconStyle;
    
    public void Save()
    {
        editorSettingsProvider.RestoreOpenTabsMode = (RestoreOpenTabsMode)restoreOpenTabsMode.SelectedOption;
        editorSettingsProvider.ToolBarButtonStyle = (ToolBarButtonStyle)toolbarIconStyle.SelectedOption;
        editorSettingsProvider.Apply();
    }

    public SettingsProvider(IGeneralEditorSettingsProvider editorSettingsProvider)
    {
        this.editorSettingsProvider = editorSettingsProvider;

        restoreOpenTabsMode = new ListOptionGenericSetting("Restore open tabs mode",
            new object[]
            {
                RestoreOpenTabsMode.RestoreWhenCrashed, RestoreOpenTabsMode.AlwaysRestore, RestoreOpenTabsMode.NeverRestore
            },
            editorSettingsProvider.RestoreOpenTabsMode, "The editor can restore opened tabs on crash, every time or never.");

        toolbarIconStyle = new ListOptionGenericSetting("Toolbar button style",
            new object[]
            {
                ToolBarButtonStyle.Icon, ToolBarButtonStyle.IconAndText, ToolBarButtonStyle.Text
            },
            editorSettingsProvider.ToolBarButtonStyle, "The style of toolbar buttons (doesn't apply to all buttons).");
        
        Settings = new List<IGenericSetting>()
        {
            restoreOpenTabsMode,
            toolbarIconStyle
        };
    }
}