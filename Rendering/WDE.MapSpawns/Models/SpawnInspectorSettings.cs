using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models;

/// <summary>
/// Persists the in-view inspector chrome (collapsed state + panel width) across restarts.
/// IUserSettings is not thread safe, so Load/Save must be called on the main thread
/// (GameViewInspector dispatches).
/// </summary>
[AutoRegister]
[SingleInstance]
public class SpawnInspectorSettings
{
    private readonly IUserSettings userSettings;

    public SpawnInspectorSettings(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
    }

    public (bool collapsed, float panelWidth) Load()
    {
        var data = userSettings.Get<Data>(new Data() { PanelWidth = 320f });
        return (data.Collapsed, data.PanelWidth);
    }

    public void Save(bool collapsed, float panelWidth) =>
        userSettings.Update(new Data() { Collapsed = collapsed, PanelWidth = panelWidth });

    public struct Data : ISettings
    {
        public bool Collapsed;
        public float PanelWidth;
    }
}
