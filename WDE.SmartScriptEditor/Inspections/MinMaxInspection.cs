using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class MinMaxInspection : IEventActionInspection
    {
        private readonly int pram;
        private readonly int pram2;
        private string parameterName;
        private string parameterName2;
        public MinMaxInspection(SmartGenericJsonData ev, int pram, int pram2)
        {
            this.pram = pram;
            this.pram2 = pram2;
            parameterName = ev.Parameters![pram].Name;
            parameterName2 = ev.Parameters![pram2].Name;
        }

        public InspectionResult? Inspect(SmartBaseElement e)
        {
            var a = e.GetParameter(pram).Value;
            var b = e.GetParameter(pram2).Value;
            if (a < 0 || b < 0 || a <= b)
                return null;
            return new InspectionResult()
            {
                Line = e.VirtualLineId,
                Message = $"`{parameterName}` must be less or equal to `{parameterName2}`",
                Severity = DiagnosticSeverity.Error
            };
        }

        public InspectionResult? Inspect(SmartAction a) => Inspect((SmartBaseElement)a);
        public InspectionResult? Inspect(SmartEvent e) => Inspect((SmartBaseElement)e);
        public InspectionResult? Inspect(SmartSource e) => Inspect((SmartBaseElement)e);
    }
}