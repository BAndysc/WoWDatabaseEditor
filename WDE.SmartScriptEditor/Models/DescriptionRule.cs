using System.Collections.Generic;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Models
{
    public class DescriptionRule
    {
        protected List<ParameterConditionalCompareAny> Conditionals = new();
        private bool inverted;

        public DescriptionRule(SmartDescriptionRulesJsonData data)
        {
            Description = data.Description;
            if (data.Conditions != null)
            {
                foreach (SmartConditionalJsonData condition in data.Conditions)
                {
                    ParameterConditionalCompareAny conditional = new(condition);

                    inverted = condition.Invert;

                    Conditionals.Add(conditional);
                }
            }
        }

        public string Description { get; }

        public bool Matches(DescriptionParams paramz)
        {
            foreach (ParameterConditionalCompareAny condition in Conditionals)
            {
                if (!condition.Validate(paramz))
                    return false;
            }

            return true;
        }
    }

    public class DescriptionParams
    {
        public long pram1;
        public long pram2;
        public long pram3;
        public long pram4;
        public long pram5;
        public long pram6;
        public long source_type;
    }
}