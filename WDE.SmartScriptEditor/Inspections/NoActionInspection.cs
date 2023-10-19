using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class NoActionInspection : IEventInspection
    {
        public InspectionResult? Inspect(SmartEvent e)
        {
            if (e.Actions.Count != 0)
                return null;
            
            return new InspectionResult()
            {
                Severity = DiagnosticSeverity.Warning,
                Message = "Event has no actions, will not be saved to database.",
                Line = e.VirtualLineId
            };
        }
    }
}