using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public class DuplicateEventsInspection : IScriptInspection
    {
        public IEnumerable<InspectionResult> Inspect(EventAiBase script)
        {
            if (script.Events.Count == 0)
                yield break;
            
            EventAiEvent previous = script.Events[0];
            for (var index = 1; index < script.Events.Count; index++)
            {
                var current = script.Events[index];

                if (current.Chance.Value == 100 && previous.Equals(current) && AllowMultipleActions(current))
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

        private bool AllowMultipleActions(EventAiEvent current)
        {
            return false; // TODO
        }
    }
}