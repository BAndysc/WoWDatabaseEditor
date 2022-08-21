using WDE.Common.Managers;

namespace WDE.EventAiEditor.Inspections
{
    public class InspectionResult : IInspectionResult
    {
        public DiagnosticSeverity Severity { get; set; }
        public string Message { get; set; } = "";
        public int Line { get; set; }
    }
}