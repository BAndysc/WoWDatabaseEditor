using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class MovementAwaitOnlyAfterMovement : IEventInspection
    {
        public InspectionResult? Inspect(SmartEvent e)
        {
            bool hasBeginInlineActionList = false;
            for (var index = 0; index < e.Actions.Count; index++)
            {
                var prev = index > 0 ? e.Actions[index - 1] : null;
                var a = e.Actions[index];
                if (a.Id == SmartConstants.ActionBeginInlineActionList)
                {
                    if (!hasBeginInlineActionList)
                        hasBeginInlineActionList = true;
                }

                if (a.Id == SmartConstants.ActionAfterMovement)
                {
                    if (prev == null)
                        return new InspectionResult()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Line = a.VirtualLineId,
                            Message = "`After previous movement` action cannot be the very first event action! It makes no sense"
                        };
                    if (!hasBeginInlineActionList)
                        return new InspectionResult()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Line = a.VirtualLineId,
                            Message = "`After previous movement` action can only work within `begin inline actionlist` at the moment"
                        };
                    if (prev.Id != SmartConstants.ActionStartWaypointsPath &&
                        prev.Id != SmartConstants.ActionMovePoint)
                        return new InspectionResult()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Line = a.VirtualLineId,
                            Message = "`After previous movement` action can only be placed after `Waypoint path start` action!"
                        };
                }
            }

            return null;
        }
    }
}