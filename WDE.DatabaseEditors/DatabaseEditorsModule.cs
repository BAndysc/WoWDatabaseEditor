using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Prism.Ioc;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Parameters;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

[assembly: InternalsVisibleTo("DatabaseTester")]

namespace WDE.DatabaseEditors
{
    [AutoRegister]
    public class DatabaseEditorsModule : ModuleBase
    {
        private IContainerProvider containerProvider = null!;

        static DatabaseEditorsModule()
        {
            var previousDefault = JsonConvert.DefaultSettings?.Invoke();
            JsonConvert.DefaultSettings = () =>
            {
                var settings = previousDefault ?? new JsonSerializerSettings();
                settings.Converters.Add(new DatabaseKeyConverter());
                return settings;
            };
        }
        
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
            containerProvider.Resolve<ILoadingEventAggregator>().OnEvent<EditorLoaded>().SubscribeOnce(_ =>
            {
                var parameterPickerService = containerProvider.Resolve<IParameterPickerService>();
                containerProvider.Resolve<IContextualParametersProvider>();
                var factory = containerProvider.Resolve<IParameterFactory>();
                factory.Register("CreatureTextTextStringParameter", containerProvider.Resolve<CreatureTextWithFallback>());
                factory.Register("BroadcastTextParameter", containerProvider.Resolve<BroadcastTextParameter>());
                factory.RegisterDepending("CreatureTemplateSpellListIdParameter", "CreatureParameter", (a) => new CreatureTemplateSpellListIdParameter(a, parameterPickerService));
                factory.RegisterDepending("DbScriptRandomTemplateTargetValueParameter", "BroadcastTextParameter", bcast => new DbScriptRandomTemplateTargetValueParameter(containerProvider.Resolve<IParameterPickerService>(), bcast));
                factory.Register("LootReferenceParameter", containerProvider.Resolve<LootReferenceParameter>());
                factory.Register("EquipmentCreatureGuidParameter", containerProvider.Resolve<EquipmentCreatureGuidParameter>());
                factory.Register("CreatureGUIDParameter", factory.Factory("TableReference(creature#guid)Parameter"));
                factory.Register("GameobjectGUIDParameter", factory.Factory("TableReference(gameobject#guid)Parameter"));
            });
        }
    }
}