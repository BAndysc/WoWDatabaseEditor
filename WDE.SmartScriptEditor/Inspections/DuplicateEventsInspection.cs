using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class DuplicateEventsInspection : IScriptInspection
    {
        public IEnumerable<InspectionResult> Inspect(SmartScriptBase script)
        {
            if (script.SourceType == SmartScriptType.TimedActionList)
                yield break;
            
            if (script.Events.Count == 0)
                yield break;
            
            SmartEvent previous = script.Events[0];
            for (var index = 1; index < script.Events.Count; index++)
            {
                var current = script.Events[index];

                if (current.Chance.Value == 100 && previous.Equals(current))
                {
                    yield return new InspectionResult()
                    {
                        Line = current.LineId,
                        Severity = DiagnosticSeverity.Info,
                        Message = "The event is a duplicate of the previous one. You can put all actions in a single event."
                    };
                }

                previous = current;
            }
        }
    }
}