using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Parameters;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

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
                containerProvider.Resolve<IContextualParametersProvider>();
                var pickerService = containerProvider.Resolve<IParameterPickerService>();
                var factory = containerProvider.Resolve<IParameterFactory>();
                factory.Register("CreatureGUIDParameter", factory.Factory("TableReference(creature#guid)Parameter"));
                factory.Register("GameobjectGUIDParameter", factory.Factory("TableReference(gameobject#guid)Parameter"));
                var cr = factory.Factory("CreatureGUIDParameter");
                var go = factory.Factory("GameobjectGUIDParameter");
                factory.Register("LinkedRespawnGuidDependantParameter", new LinkedRespawnGuidParameter(false, cr, go, pickerService));
                factory.Register("LinkedRespawnGuidMasterParameter", new LinkedRespawnGuidParameter(true, cr, go, pickerService));
                factory.Register("InstanceSpawnInfoSpawnIdParameter", new InstanceSpawnInfoSpawnIdParameter(cr, go, pickerService));
                factory.RegisterCombined("TrainerRequirementParameter", "ClassParameter", "RaceParameter", "SpellParameter", (@class, race, spell) => new TrainerRequirementParameter(@class, race, spell, pickerService));
                factory.RegisterCombined("InstanceEncounterCreatureSpellParameter", "CreatureParameter", "SpellParameter", (cr, sp) => new InstanceEncounterCreatureSpellParameter(cr, sp, pickerService));
                factory.RegisterCombined("DisablesEntryParameter", "SpellParameter", "QuestParameter", "MapParameter", "BattlegroundParameter", "AchievementCriteriaParameter", (s, q, m, b, a) => new DisablesEntryParameter(s, q, m, b, a, pickerService));
                factory.RegisterCombined("DisablesFlagsParameter", "DisableTypeSpellFlagsParameter", "DisableTypeMapFlagsParameter", "DisableTypeVMapFlagsParameter", (s, m, vm) => new DisablesFlagsParameter(s, m, vm, pickerService));
            });
        }
    }
}