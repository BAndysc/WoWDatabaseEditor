using WDE.Common.Managers;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public class IndentationInspection : IEventInspection
    {
        public InspectionResult? Inspect(EventAiEvent e)
        {
            int indentation = 0;
            foreach (var action in e.Actions)
            {
                if (action.ActionFlags.HasFlagFast(ActionFlags.DecreaseIndent))
                {
                    if (indentation <= 0)
                    {
                        return new InspectionResult()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Line = action.LineId,
                            Message = "[END] without matching begin block, script will not work at all"
                        };
                    }
                    indentation--;
                }

                if (action.ActionFlags.HasFlagFast(ActionFlags.IncreaseIndent))
                    indentation++;
            }

            if (indentation != 0)
            {
                return new InspectionResult()
                {
                    Severity = DiagnosticSeverity.Error,
                    Line = e.Actions[^1].LineId,
                    Message = "Missing [END] action, script will not work at all."
                };
            }

            return null;
        }
    }
}