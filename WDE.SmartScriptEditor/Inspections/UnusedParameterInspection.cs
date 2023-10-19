using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class UnusedParameterInspection : IActionInspection, IEventInspection, IEventInspectionFix, IActionInspectionFix
    {
        public InspectionResult? Inspect(SmartBaseElement e, string ofWhat)
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
                        Line = e.VirtualLineId
                    };
                }
            }

            return null;
        }
        
        public InspectionResult? Inspect(SmartAction a)
        {
            return Inspect((SmartBaseElement)a, "action");
        }

        public InspectionResult? Inspect(SmartEvent e)
        {
            return Inspect((SmartBaseElement)e, "event");
        }

        public void Fix(SmartBaseElement e)
        {
            for (int i = 0; i < e.ParametersCount; ++i)
            {
                if (!e.GetParameter(i).IsUsed && e.GetParameter(i).Value != 0)
                {
                    e.GetParameter(i).Value = 0;
                }
            }
        }
        
        public void Fix(SmartEvent e)
        {
            Fix((SmartBaseElement) e);
        }

        public void Fix(SmartAction action)
        {
            Fix((SmartBaseElement) action);
        }
    }
}