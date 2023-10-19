using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections;

public class RepeatTimedActionListOnlyInBeginInlineTimedActionListInspection : IEventInspection
{
    public InspectionResult? Inspect(SmartEvent e)
    {
        bool hasBeginInlineActionList = false;
        for (var index = 0; index < e.Actions.Count; index++)
        {
            var prev = index > 0 ? e.Actions[index - 1] : null;
            var a = e.Actions[index];
            if (a.Id == SmartConstants.ActionBeginInlineActionList)
                hasBeginInlineActionList = true;

            if (a.Id == SmartConstants.ActionRepeatTimedActionList)
            {
                if (!hasBeginInlineActionList)
                    return new InspectionResult()
                    {
                        Severity = DiagnosticSeverity.Error,
                        Line = a.VirtualLineId,
                        Message = "`Repeat actionlist` action can only work within `begin inline actionlist`"
                    };
                if (index != e.Actions.Count - 1)
                    return new InspectionResult()
                    {
                        Severity = DiagnosticSeverity.Error,
                        Line = a.VirtualLineId,
                        Message = "`Repeat actionlist` action must be the last action of the event, otherwise the next events will be forgotten"
                    };
            }
        }

        return null;
    }
}