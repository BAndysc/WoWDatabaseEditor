using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Prism.Ioc;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Structs;
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

        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
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
                factory.Register("GossipOptionTextStringParameter", containerProvider.Resolve<GossipOptionTextWithFallback>());
                factory.Register("VehicleSeatIdParameter", this.containerProvider.Resolve<VehicleSeatIdParameter>());
                factory.Register("BroadcastTextOnlyPickerParameter", containerProvider.Resolve<BroadcastTextOnlyPickerParameter>());
                factory.Register("BroadcastTextParameter", containerProvider.Resolve<BroadcastTextParameter>());
                factory.RegisterDepending("CreatureTemplateSpellListIdParameter", "CreatureParameter", (a) => new CreatureTemplateSpellListIdParameter(a, parameterPickerService));
                factory.RegisterDepending("DbScriptRandomTemplateTargetValueParameter", "BroadcastTextParameter", bcast => new DbScriptRandomTemplateTargetValueParameter(containerProvider.Resolve<IParameterPickerService>(), bcast));
                factory.Register("EquipmentCreatureGuidParameter", containerProvider.Resolve<EquipmentCreatureGuidParameter>());
                factory.Register("CreatureGUIDParameter", this.containerProvider.Resolve<CreatureGUIDParameter>());
                factory.Register("QuestObjectiveByStorageIndex(entry)Parameter",this.containerProvider.Resolve<QuestObjectiveByStorageIndexParameter>((typeof(ColumnFullName), new ColumnFullName(null, "entry"))));
                factory.Register("GameobjectGUIDParameter", this.containerProvider.Resolve<GameObjectGUIDParameter>());
                factory.Register("CreatureGUID(PickerOnly)Parameter", this.containerProvider.Resolve<CreatureGUIDPickerOnlyParameter>());
                factory.Register("GameobjectGUID(PickerOnly)Parameter", this.containerProvider.Resolve<GameObjectGUIDPickerOnlyParameter>());
                factory.Register("CreatureGUID(EntryOnly)Parameter", this.containerProvider.Resolve<CreatureGUIDEntryOnlyParameter>());
            });
        }
    }
}