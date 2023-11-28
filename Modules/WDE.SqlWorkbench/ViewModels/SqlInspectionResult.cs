using WDE.Common.Managers;

namespace WDE.SqlWorkbench.ViewModels;

public class SqlInspectionResult : IInspectionResult
{
    public SqlInspectionResult(string message, int line)
    {
        Message = message;
        Line = line;
    }

    public string Message { get; }
    public DiagnosticSeverity Severity => DiagnosticSeverity.Error;
    public int Line { get; }
}