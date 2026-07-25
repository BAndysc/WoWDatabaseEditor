using System.Collections.Generic;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models;

/// <summary>
/// Persists the spawns-tree view state across restarts (expansion state + the "only the loaded map"
/// filter). IUserSettings is not thread safe, so BOTH Load and Save must be called on the main thread
/// (the tree window dispatches). Expand keys are the tree's stable node-path tokens, values are
/// (int)ExpandState.
/// </summary>
[AutoRegister]
[SingleInstance]
public class SpawnsTreeSettings
{
    private readonly IUserSettings userSettings;

    public SpawnsTreeSettings(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
    }

    public Dictionary<string, int>? LoadExpandState() => userSettings.Get<Data>(new Data()).ExpandState;

    // the two settings share one blob, so each save reads-modifies-writes to keep the other field
    public void SaveExpandState(Dictionary<string, int> state)
    {
        var data = userSettings.Get<Data>(new Data());
        data.ExpandState = state;
        userSettings.Update(data);
    }

    public bool LoadOnlyLoadedMap() => userSettings.Get<Data>(new Data()).OnlyLoadedMap;

    public void SaveOnlyLoadedMap(bool value)
    {
        var data = userSettings.Get<Data>(new Data());
        data.OnlyLoadedMap = value;
        userSettings.Update(data);
    }

    public struct Data : ISettings
    {
        public Dictionary<string, int>? ExpandState;
        public bool OnlyLoadedMap;
    }
}
