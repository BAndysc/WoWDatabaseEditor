using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Parameters;

namespace WDE.Conditions.ViewModels
{
    public class ConditionObjectEntryParameter : IContextualParameter<long, ConditionViewModel>
    {
        private readonly IParameterFactory parameterFactory;
        public bool HasItems => true;

        public ConditionObjectEntryParameter(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
        }
        
        public string ToString(long value)
        {
            return value.ToString();
        }

        public Dictionary<long, SelectOption>? Items { get; }

        public Dictionary<long, SelectOption>? ItemsForContext(ConditionViewModel context)
        {
            LazyLoad();
            
            if (context.ConditionValue1.Value == 3)
                return creatureParameter!.Items;
            
            if (context.ConditionValue1.Value == 5)
                return gameobjectParameter!.Items;
            
            return null;
        }

        private void LazyLoad()
        {
            if (creatureParameter == null || gameobjectParameter == null)
            {
                creatureParameter = parameterFactory.Factory("CreatureParameter");
                gameobjectParameter = parameterFactory.Factory("GameobjectParameter");
            }
        }

        private IParameter<long>? creatureParameter;
        private IParameter<long>? gameobjectParameter;

        public string ToString(long value, ConditionViewModel context)
        {
            LazyLoad();
            
            if (context.ConditionValue1.Value == 3)
                return creatureParameter!.ToString(value);

            if (context.ConditionValue1.Value == 5)
                return gameobjectParameter!.ToString(value);

            return ToString(value);
        }
    }
}