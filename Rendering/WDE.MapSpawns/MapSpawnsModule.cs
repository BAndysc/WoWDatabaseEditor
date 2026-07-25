using Prism.Events;
using Prism.Ioc;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.MapRenderer;
using WDE.MapSpawns.Rendering;
using WDE.MapSpawns.Rendering.AreaTriggers;
using WDE.MapSpawns.Rendering.CreatureLinking;
using WDE.MapSpawns.Rendering.Formations;
using WDE.MapSpawns.Rendering.Pools;
using WDE.MapSpawns.Rendering.SpawnGroups;
using WDE.MapSpawns.Rendering.Waypoints;
using WDE.MapSpawns.Rendering.WorldPoints;
using WDE.Module;

namespace WDE.MapSpawns;

public class MapSpawnsModule : ModuleBase
{
    public override void OnInitialized(IContainerProvider containerProvider)
    {
        base.OnInitialized(containerProvider);
        containerProvider.Resolve<IEventAggregator>()
            .GetEvent<AllModulesLoaded>()
            .Subscribe(() =>
                {
                    var gameView = containerProvider.Resolve<IGameView>();
                    // waypoint + formation editors first: their Update consumes world clicks before
                    // SpawnViewer reacts
                    gameView.RegisterGameModule(c => c.Resolve<WaypointEditorModule>());
                    gameView.RegisterGameModule(c => c.Resolve<FormationEditorModule>());
                    gameView.RegisterGameModule(c => c.Resolve<SpawnGroupEditorModule>());
                    gameView.RegisterGameModule(c => c.Resolve<PoolEditorModule>());
                    gameView.RegisterGameModule(c => c.Resolve<SafeLocEditorModule>());
                    gameView.RegisterGameModule(c => c.Resolve<SpellTargetEditorModule>());
                    gameView.RegisterGameModule(c => c.Resolve<AreaTriggerEditorModule>());
                    gameView.RegisterGameModule(c => c.Resolve<CreatureLinkEditorModule>());
                    gameView.RegisterGameModule(c => c.Resolve<SpawnViewer>());
                },
                ThreadOption.PublisherThread,
                true);
    }
}