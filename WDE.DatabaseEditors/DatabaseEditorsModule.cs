using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors
{
    [AutoRegister]
    public class DatabaseEditorsModule : ModuleBase
    {
        private IContainerProvider containerProvider = null!;
        
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
            containerProvider.Resolve<ILoadingEventAggregator>().OnEvent<EditorLoaded>().SubscribeOnce(_ =>
            {
                containerProvider.Resolve<IParameterFactory>().RegisterCombined("TrainerRequirementParameter", "ClassParameter", "RaceParameter", "SpellParameter", (@class, race, spell) => new TrainerRequirementParameter(@class, race, spell, containerProvider.Resolve<IParameterPickerService>()));
            });
        }

        internal class TrainerRequirementParameter : IContextualParameter<long, DatabaseEntity>, ICustomPickerContextualParameter<long>
        {
            private readonly IParameter<long> @class;
            private readonly IParameter<long> race;
            private readonly IParameter<long> spell;
            private readonly IParameterPickerService parameterPickerService;

            public TrainerRequirementParameter(IParameter<long> @class, 
                IParameter<long> race, 
                IParameter<long> spell,
                IParameterPickerService parameterPickerService)
            {
                this.@class = @class;
                this.race = race;
                this.spell = spell;
                this.parameterPickerService = parameterPickerService;
            }

            public Task<(long, bool)> PickValue(long value, object context)
            {
                if (context is DatabaseEntity entity)
                {
                    var type = entity.GetTypedValueOrThrow<long>("Type");
                    if (type == 0) // class
                        return parameterPickerService.PickParameter(@class, value);
                    if (type == 1) // race
                        return parameterPickerService.PickParameter(race, value);
                    if (type == 2) // spell
                        return parameterPickerService.PickParameter(spell, value);
                }
                return parameterPickerService.PickParameter(Parameter.Instance, value);
            }

            public string? Prefix => null;
            public bool HasItems => true;
            
            public string ToString(long value)
            {
                return value.ToString();
            }

            public Dictionary<long, SelectOption>? Items => null;

            public Dictionary<long, SelectOption>? ItemsForContext(DatabaseEntity context)
            {
                var type = context.GetTypedValueOrThrow<long>("Type");
                if (type == 0) // class
                    return @class.Items;
                if (type == 1) // race
                    return race.Items;
                if (type == 2) // spell
                    return spell.Items;
                return Items;
            }

            public string ToString(long value, DatabaseEntity context)
            {
                var type = context.GetTypedValueOrThrow<long>("Type");
                if (type == 0) // class
                    return @class.ToString(value);
                if (type == 1) // race
                    return race.ToString(value);
                if (type == 2) // spell
                    return spell.ToString(value);
                return value.ToString();
            }
        }

    }
}