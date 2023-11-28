using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Settings;

[UniqueProvider]
public interface IGeneralEditorSettingsProvider
{
    RestoreOpenTabsMode RestoreOpenTabsMode { get; set; }
    void Apply();
}

public enum RestoreOpenTabsMode
{
    RestoreWhenCrashed,
    AlwaysRestore,
    NeverRestore
}