using System.Runtime.CompilerServices;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Conditions.ViewModels;
using WDE.Module;
using WDE.Module.Attributes;

[assembly: InternalsVisibleTo("WDE.Conditions.Avalonia")]
[assembly: ModuleRequiresCore("TrinityWrath", "TrinityCata", "TrinityMaster", "Azeroth")]
namespace WDE.Conditions
{
    public class ConditionsModule : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<IEventAggregator>()
                .GetEvent<AllModulesLoaded>()
                .Subscribe(() =>
                    {
                        var factory = containerProvider.Resolve<IParameterFactory>();
                        factory.Register("ConditionObjectEntryParameter", new ConditionObjectEntryParameter(factory));
                        factory.Register("ConditionObjectGuidParameter", new Parameter());
                    },
                    ThreadOption.PublisherThread,
                    true);
        }
    }
}