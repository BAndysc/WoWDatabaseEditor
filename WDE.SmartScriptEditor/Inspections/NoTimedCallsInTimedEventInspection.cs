using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class NoTimedCallsInTimedEventInspection : IActionInspection
    {
        public InspectionResult? Inspect(SmartAction a)
        {
            if (a.Parent == null)
                return null;

            if (a.Id != SmartConstants.ActionCreateTimed)
                return null;

            if (a.Parent.Id != SmartConstants.EventTriggerTimed)
                return null;

            return new InspectionResult()
            {
                Severity = DiagnosticSeverity.Error,
                Line = a.VirtualLineId,
                Message = "You may not create timed event inside a timed event, core will crash"
            };
        }
    }
}