using System;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Validation.Antlr;

namespace WDE.SmartScriptEditor.Inspections
{
    public class JsonConditionInspection : IEventActionInspection
    {
        private readonly SmartRule rule;
        private SmartValidator validator;
        public JsonConditionInspection(SmartRule rule)
        {
            this.rule = rule;
            validator = new SmartValidator(rule.Rule);
        }
        
        public InspectionResult? Inspect(SmartEvent ev)
        {
            var valid = validator.Evaluate(new SmartValidationContext(ev.Parent!, ev, null));
            if (!valid)
            {
                return new InspectionResult()
                {
                    Severity = rule.Level,
                    Message = rule.Description,
                    Line = ev.VirtualLineId
                };
            }
            return null;
        }

        public InspectionResult? Inspect(SmartBaseElement element) => throw new NotImplementedException();

        public InspectionResult? Inspect(SmartAction a)
        {
            var valid = validator.Evaluate(new SmartValidationContext(a.Parent!.Parent!, a.Parent, a));
            if (!valid)
            {
                return new InspectionResult()
                {
                    Severity = rule.Level,
                    Message = rule.Description,
                    Line = a.VirtualLineId
                };
            }
            return null;
        }

        public InspectionResult? Inspect(SmartSource e) => throw new Exception("Not supported yet");
    }
}