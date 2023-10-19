using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class BeginInlineActionListOnlyOnceInspection : IEventInspection
    {
        public InspectionResult? Inspect(SmartEvent e)
        {
            bool hasBeginInlineActionList = false;
            foreach (var a in e.Actions)
            {
                if (a.Id == SmartConstants.ActionBeginInlineActionList)
                {
                    if (!hasBeginInlineActionList)
                        hasBeginInlineActionList = true;
                    else
                        return new InspectionResult()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Line = a.VirtualLineId,
                            Message = "`Begin inline action list` can be used only ONCE in a single event!"
                        };
                }
            }
            return null;
        }
    }
}