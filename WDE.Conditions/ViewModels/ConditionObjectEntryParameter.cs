using System.Collections.Generic;
using WDE.Common.Parameters;
using WDE.Conditions.Shared;

namespace WDE.Conditions.ViewModels
{
    public class ConditionObjectEntryParameter : BaseContextualParameter<long, IConditionViewModel>
    {
        private readonly IParameterFactory parameterFactory;
        public override string? Prefix => null;
        public override bool HasItems => true;

        public ConditionObjectEntryParameter(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
        }
        
        public override string ToString(long value)
        {
            return value.ToString();
        }

        public override Dictionary<long, SelectOption>? Items => null;

        public Dictionary<long, SelectOption>? ItemsForContext(IConditionViewModel context)
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

        public override string ToString(long value, IConditionViewModel context)
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