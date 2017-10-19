using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Models
{
    public class DescriptionRule
    {
        public string Description { get; }
        private bool _inverted = false;
        protected List<ParameterConditionalCompareAny> Conditionals = new List<ParameterConditionalCompareAny>();
        
        public bool Matches(DescriptionParams paramz)
        {
            foreach (ParameterConditionalCompareAny condition in Conditionals)
            {
                if (!condition.Validate(paramz))
                    return false;
            }

            return true;
        }

        public DescriptionRule(SmartDescriptionRulesJsonData data)
        {
            Description = data.Description;
            if (data.Conditions != null)
            {
                foreach (SmartConditionalJsonData condition in data.Conditions)
                {
                    ParameterConditionalCompareAny conditional = new ParameterConditionalCompareAny(condition);

                    _inverted = condition.Invert;

                    Conditionals.Add(conditional);
                }
            }
        }
    }

    public class DescriptionParams
    {
        public int source_type;
        public int pram1;
        public int pram2;
        public int pram3;
        public int pram4;
        public int pram5;
        public int pram6;
    }
}
