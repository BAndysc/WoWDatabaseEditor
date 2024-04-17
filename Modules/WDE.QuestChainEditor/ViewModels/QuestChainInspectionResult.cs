using System.Collections.Generic;
using System.Linq;
using WDE.Common.Managers;

namespace WDE.QuestChainEditor.ViewModels;

public class QuestChainInspectionResult : IInspectionResult
{
    public QuestChainInspectionResult(int line, DiagnosticSeverity severity, string message, IEnumerable<BaseQuestViewModel> affected)
    {
        Line = line;
        Severity = severity;
        Affected = affected.ToList();
        Message = message + " (affected quests: "+string.Join(", ", Affected.Select(x => x.ToString()))+")";
    }

    public string Message { get; }
    public DiagnosticSeverity Severity { get; }
    public IReadOnlyList<BaseQuestViewModel> Affected { get; }
    public int Line { get; }
}