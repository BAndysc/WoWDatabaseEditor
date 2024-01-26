using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Settings;

[UniqueProvider]
public interface IGeneralEditorSettingsProvider
{
    RestoreOpenTabsMode RestoreOpenTabsMode { get; set; }
    ToolBarButtonStyle ToolBarButtonStyle { get; set; }
    void Apply();
}

public enum RestoreOpenTabsMode
{
    RestoreWhenCrashed,
    AlwaysRestore,
    NeverRestore
}

public enum ToolBarButtonStyle
{
    Icon,
    IconAndText,
    Text
}