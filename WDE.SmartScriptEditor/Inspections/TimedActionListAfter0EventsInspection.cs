using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class TimedActionListAfter0EventsInspection : IScriptInspection, IScriptInspectionFix
    {
        private bool CanInspect(SmartScriptBase script)
        {
            if (script.SourceType != SmartScriptType.TimedActionList)
                return false;

            if (script.Events.Count <= 1)
                return false;

            return true;
        }
        
        public IEnumerable<InspectionResult> Inspect(SmartScriptBase script)
        {
            if (!CanInspect(script))
                yield break;
            
            for (var index = 1; index < script.Events.Count; index++)
            {
                var current = script.Events[index];

                if (current.Actions.Count == 0)
                    continue;

                if (current.GetParameter(0).Value == 0 && current.Id == script.Events[index - 1].Id)
                {
                    yield return new InspectionResult()
                    {
                        Line = current.VirtualLineId,
                        Severity = DiagnosticSeverity.Info,
                        Message = "This event can be merged with the previous one."
                    };
                }
            }
        }

        public void Fix(SmartScriptBase script)
        {
            if (!CanInspect(script))
                return;
            
            for (var index = 1; index < script.Events.Count; index++)
            {
                var current = script.Events[index];

                if (current.Actions.Count == 0)
                    continue;

                if (current.GetParameter(0).Value == 0)
                {
                    var previous = script.Events[index - 1];

                    foreach (var action in current.Actions)
                        previous.Actions.Add(action.Copy());
                    for (int i = current.Actions.Count - 1; i >= 0; --i)
                        current.Actions.RemoveAt(i);
                    script.Events.RemoveAt(index);

                    index--;
                }
            }
        }
    }
}