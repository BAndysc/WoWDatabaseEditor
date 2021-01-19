using Prism.Events;
using Prism.Ioc;
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
                        containerProvider.Resolve<ParameterLoader>().Load(containerProvider
                            .Resolve<ParameterFactory>());
                    },
                    ThreadOption.PublisherThread,
                    true);
        }
    }
}