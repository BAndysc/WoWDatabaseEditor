using WDE.Common.Managers;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public class UnusedParameterInspection : IActionInspection, IEventInspection, IEventInspectionFix, IActionInspectionFix
    {
        public InspectionResult? Inspect(EventAiBaseElement e, string ofWhat)
        {
            for (int i = 0; i < e.ParametersCount; ++i)
            {
                if (!e.GetParameter(i).IsUsed && e.GetParameter(i).Value != 0)
                {
                    int index = i + 1;
                    string nd = index == 1 ? "st" : (index == 2 ? "nd" : (index == 3 ? "rd" : "th"));
                    return new InspectionResult()
                    {
                        Severity = DiagnosticSeverity.Info,
                        Message = $" {index}{nd} parameter of {ofWhat} is not used, but has non zero value.",
                        Line = e.LineId
                    };
                }
            }

            return null;
        }
        
        public InspectionResult? Inspect(EventAiAction a)
        {
            return Inspect((EventAiBaseElement)a, "action");
        }

        public InspectionResult? Inspect(EventAiEvent e)
        {
            return Inspect((EventAiBaseElement)e, "event");
        }

        public void Fix(EventAiBaseElement e)
        {
            for (int i = 0; i < e.ParametersCount; ++i)
            {
                if (!e.GetParameter(i).IsUsed && e.GetParameter(i).Value != 0)
                {
                    e.GetParameter(i).Value = 0;
                }
            }
        }
        
        public void Fix(EventAiEvent e)
        {
            Fix((EventAiBaseElement) e);
        }

        public void Fix(EventAiAction action)
        {
            Fix((EventAiBaseElement) action);
        }
    }
}