using Prism.Events;
using Prism.Ioc;
using WDE.Common.Events;
using WDE.Module;
using WDE.Module.Attributes;

namespace WDE.DbcStore
{
    [AutoRegister]
    public class DbcStoreModule : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            /*ontainerProvider.Resolve<IEventAggregator>()
                .GetEvent<AllModulesLoaded>()
                .Subscribe(() => { containerProvider.Resolve<DbcStore>().Load(); }, ThreadOption.PublisherThread, true);*/
        }
    }
}