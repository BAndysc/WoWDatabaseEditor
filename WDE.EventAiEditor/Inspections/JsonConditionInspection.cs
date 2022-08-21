using System;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Models;
using WDE.EventAiEditor.Validation.Antlr;

namespace WDE.EventAiEditor.Inspections
{
    public class JsonConditionInspection : IEventActionInspection
    {
        private readonly EventAiRule rule;
        private EventAiValidator validator;
        public JsonConditionInspection(EventAiRule rule)
        {
            this.rule = rule;
            validator = new EventAiValidator(rule.Rule);
        }
        
        public InspectionResult? Inspect(EventAiEvent ev)
        {
            var valid = validator.Evaluate(new EventAiValidationContext(ev.Parent!, ev, null));
            if (!valid)
            {
                return new InspectionResult()
                {
                    Severity = rule.Level,
                    Message = rule.Description,
                    Line = ev.LineId
                };
            }
            return null;
        }

        public InspectionResult? Inspect(EventAiBaseElement element) => throw new NotImplementedException();

        public InspectionResult? Inspect(EventAiAction a)
        {
            var valid = validator.Evaluate(new EventAiValidationContext(a.Parent!.Parent!, a.Parent, a));
            if (!valid)
            {
                return new InspectionResult()
                {
                    Severity = rule.Level,
                    Message = rule.Description,
                    Line = a.LineId
                };
            }
            return null;
        }
    }
}