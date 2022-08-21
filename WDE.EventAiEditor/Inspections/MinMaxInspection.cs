using WDE.Common.Managers;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public class MinMaxInspection : IEventActionInspection
    {
        private readonly int pram;
        private readonly int pram2;
        private string parameterName;
        private string parameterName2;
        public MinMaxInspection(EventActionGenericJsonData ev, int pram, int pram2)
        {
            this.pram = pram;
            this.pram2 = pram2;
            parameterName = ev.Parameters![pram].Name;
            parameterName2 = ev.Parameters![pram2].Name;
        }

        public InspectionResult? Inspect(EventAiBaseElement e)
        {
            if (e.GetParameter(pram).Value <= e.GetParameter(pram2).Value)
                return null;
            return new InspectionResult()
            {
                Line = e.LineId,
                Message = $"`{parameterName}` must be less or equal to `{parameterName2}`",
                Severity = DiagnosticSeverity.Error
            };
        }

        public InspectionResult? Inspect(EventAiAction a) => Inspect((EventAiBaseElement)a);
        public InspectionResult? Inspect(EventAiEvent e) => Inspect((EventAiBaseElement)e);
    }
}