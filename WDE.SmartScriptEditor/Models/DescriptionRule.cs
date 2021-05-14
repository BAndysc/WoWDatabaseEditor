using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Validation;
using WDE.SmartScriptEditor.Validation.Antlr;

namespace WDE.SmartScriptEditor.Models
{
    public class DescriptionRule
    {
        private SmartValidator? validator;

        public DescriptionRule(SmartDescriptionRulesJsonData data)
        {
            Description = data.Description;
            validator = data.Condition == null ? null : new SmartValidator(data.Condition);
        }

        public string Description { get; }

        public bool Matches(ISmartValidationContext context)
        {
            if (validator == null)
                return true;
            
            return validator.Evaluate(context);
        }
    }
}