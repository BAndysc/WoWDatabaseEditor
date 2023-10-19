using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class PercentValueInspection : IEventActionInspection
    {
        private readonly int pram;
        private string parameterName;
        public PercentValueInspection(SmartGenericJsonData ev, int pram)
        {
            this.pram = pram;
            parameterName = ev.Parameters![pram].Name;
        }

        public InspectionResult? Inspect(SmartBaseElement e)
        {
            if (e.GetParameter(pram).Value <= 100)
                return null;
            
            return new InspectionResult()
            {
                Line = e.VirtualLineId,
                Message = $"`{parameterName}` is percent and can not be greater than 100%.",
                Severity = DiagnosticSeverity.Error
            };
        }

        public InspectionResult? Inspect(SmartAction a) => Inspect((SmartBaseElement)a);
        public InspectionResult? Inspect(SmartEvent e) => Inspect((SmartBaseElement)e);
        public InspectionResult? Inspect(SmartSource e) => Inspect((SmartBaseElement)e);
    }
}