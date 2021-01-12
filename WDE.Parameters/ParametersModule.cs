using Prism.Events;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Module;
using WDE.Module.Attributes;

namespace WDE.Parameters
{
    [AutoRegister]
    public class ParametersModule : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<IEventAggregator>()
                .GetEvent<AllModulesLoaded>()
                .Subscribe(() =>
                    {
                        new ParameterLoader(containerProvider.Resolve<IDatabaseProvider>()).Load(containerProvider
                            .Resolve<ParameterFactory>());
                    },
                    ThreadOption.PublisherThread,
                    true);
        }
    }
}