using Prism.Events;
using Prism.Ioc;
using WDE.Common.Events;
using WDE.Module;

namespace WDE.MapSpawns.Bridge;

/// <summary>
/// Full-editor-only module that wires the game-side spawn edit requests (published by
/// <c>WDE.MapSpawns.Models.WorldSpawnEditClient</c>) to the real DatabaseEditors table documents,
/// giving spawns/deletes proper SQL + session + undo. RenderingTester does not reference this module,
/// so there the requests go unanswered and the edit UI stays disabled.
/// </summary>
public class MapSpawnsBridgeModule : ModuleBase
{
    public override void OnInitialized(IContainerProvider containerProvider)
    {
        base.OnInitialized(containerProvider);
        // activate once everything is registered - the bridge subscribes to the request events and
        // publishes the initial "available" state so the game UI shows the edit affordances.
        containerProvider.Resolve<IEventAggregator>()
            .GetEvent<AllModulesLoaded>()
            .Subscribe(() =>
                {
                    containerProvider.Resolve<WorldSpawnEditBridge>().Activate();
                    containerProvider.Resolve<SpawnScriptsBridge>().Activate();
                },
                ThreadOption.PublisherThread,
                true);
    }
}
