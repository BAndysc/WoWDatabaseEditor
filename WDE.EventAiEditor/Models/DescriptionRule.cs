using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Validation;
using WDE.EventAiEditor.Validation.Antlr;

namespace WDE.EventAiEditor.Models
{
    public class DescriptionRule
    {
        private EventAiValidator? validator;

        public DescriptionRule(EventDescriptionRulesJsonData data)
        {
            Description = data.Description;
            validator = data.Condition == null ? null : new EventAiValidator(data.Condition);
        }

        public string Description { get; }

        public bool Matches(IEventAiValidationContext context)
        {
            if (validator == null)
                return true;
            
            return validator.Evaluate(context);
        }
    }
}