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
        public int pram1;
        public int pram2;
        public int pram3;
        public int pram4;
        public int pram5;
        public int pram6;
        public int source_type;
    }
}