using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class NonZeroInspection : IEventActionInspection
    {
        private readonly int pram;
        private string parameterName;
        public NonZeroInspection(SmartGenericJsonData ev, int pram)
        {
            this.pram = pram;
            parameterName = ev.Parameters![pram].Name;
        }

        public InspectionResult? Inspect(SmartBaseElement e)
        {
            if (e.GetParameter(pram).Value != 0)
                return null;
            return new InspectionResult()
            {
                Line = e.VirtualLineId,
                Message = $"`{parameterName}` can not be 0.",
                Severity = DiagnosticSeverity.Error
            };
        }

        public InspectionResult? Inspect(SmartAction a) => Inspect((SmartBaseElement)a);
        public InspectionResult? Inspect(SmartEvent e) => Inspect((SmartBaseElement)e);
        public InspectionResult? Inspect(SmartSource e) => Inspect((SmartBaseElement)e);
    }
}