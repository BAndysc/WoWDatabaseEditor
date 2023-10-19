using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class IndentationInspection : IEventInspection
    {
        public InspectionResult? Inspect(SmartEvent e)
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
                            Line = action.VirtualLineId,
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
                    Line = e.Actions[^1].VirtualLineId,
                    Message = "Missing [END] action, script will not work at all."
                };
            }

            return null;
        }
    }
}