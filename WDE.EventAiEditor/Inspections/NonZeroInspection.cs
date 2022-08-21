using WDE.Common.Managers;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public class NonZeroInspection : IEventActionInspection
    {
        private readonly int pram;
        private string parameterName;
        public NonZeroInspection(EventActionGenericJsonData ev, int pram)
        {
            this.pram = pram;
            parameterName = ev.Parameters![pram].Name;
        }

        public InspectionResult? Inspect(EventAiBaseElement e)
        {
            if (e.GetParameter(pram).Value != 0)
                return null;
            return new InspectionResult()
            {
                Line = e.LineId,
                Message = $"`{parameterName}` can not be 0.",
                Severity = DiagnosticSeverity.Error
            };
        }

        public InspectionResult? Inspect(EventAiAction a) => Inspect((EventAiBaseElement)a);
        public InspectionResult? Inspect(EventAiEvent e) => Inspect((EventAiBaseElement)e);
    }
}