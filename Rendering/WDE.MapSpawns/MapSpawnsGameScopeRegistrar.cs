using Prism.Ioc;
using WDE.MapRenderer;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.AreaTriggers;
using WDE.MapSpawns.Models.CreatureLinking;
using WDE.MapSpawns.Models.Formations;
using WDE.MapSpawns.Models.Pools;
using WDE.MapSpawns.Models.SpawnGroups;
using WDE.MapSpawns.Models.Waypoints;
using WDE.MapSpawns.Models.WorldPoints;
using WDE.MapSpawns.Rendering;
using WDE.Module.Attributes;

namespace WDE.MapSpawns;

/// <summary>
/// Registers the spawn editor services into the per-game scoped container. They are deliberately
/// NOT [AutoRegister]: they hold per-map state (spawns, paths, selections), so a global registration
/// would keep all of it alive after the game view closes. Registered here, each is a singleton
/// within one game session and is released when the game scope is disposed.
/// </summary>
[AutoRegister]
[SingleInstance]
public class MapSpawnsGameScopeRegistrar : IGameScopeRegistrar
{
    public void RegisterScopedTypes(IContainerRegistry gameScope)
    {
        gameScope.RegisterSingleton<ISpawnsContainer, SpawnsContainer>();
        gameScope.RegisterSingleton<ISpawnSelectionService, SpawnSelectionService>();
        gameScope.RegisterSingleton<ISpawnEditorToolService, SpawnEditorToolService>();
        gameScope.RegisterSingleton<IGameViewOverlayService, GameViewOverlayService>();
        gameScope.RegisterSingleton<IGameNotificationService, GameNotificationService>();
        gameScope.RegisterSingleton<ISpawnScriptsService, SpawnScriptsClient>();
        gameScope.RegisterSingleton<IWorldSpawnEditService, WorldSpawnEditClient>();
        gameScope.RegisterSingleton<IWaypointEditorService, WaypointEditorService>();
        gameScope.RegisterSingleton<IFormationEditorService, FormationEditorService>();
        gameScope.RegisterSingleton<ICreatureLinkEditorService, CreatureLinkEditorService>();
        gameScope.RegisterSingleton<IPoolEditorService, PoolEditorService>();
        gameScope.RegisterSingleton<ISpawnGroupEditorService, SpawnGroupEditorService>();
        gameScope.RegisterSingleton<ISafeLocEditorService, SafeLocEditorService>();
        gameScope.RegisterSingleton<ISpellTargetEditorService, SpellTargetEditorService>();
        gameScope.RegisterSingleton<IAreaTriggerEditorService, AreaTriggerEditorService>();
        gameScope.RegisterSingleton<EntryPickerService>();
        gameScope.RegisterSingleton<SpawnEditorTutorial>();
    }
}
