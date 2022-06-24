using Prism.Events;
using Prism.Ioc;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.MapRenderer;
using WDE.MapSpawns.Rendering;
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
                    containerProvider.Resolve<IGameView>().RegisterGameModule(c => c.Resolve<SpawnViewer>());
                },
                ThreadOption.PublisherThread,
                true);
    }
}