using System.Windows.Input;
using WDE.Common.Managers;
using WDE.Common.Utils;

namespace WoWDatabaseEditorCore.Services.ProblemsTool
{
    public class ProblemViewModel
    {
        public ProblemViewModel(IInspectionResult problem)
        {
            Message = problem.Message;
            Severity = problem.Severity;
            LineNumber = problem.Line;
            Fix = AlwaysDisabledCommand.Command;
            Line = $"Line " + LineNumber;
        }

        public int LineNumber { get; set; }
        public string Line { get; set; }
        public string Message { get; set; }
        public ICommand? Fix { get; set; }
        public DiagnosticSeverity Severity { get; set; }
    }
}