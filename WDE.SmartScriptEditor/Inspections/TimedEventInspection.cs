using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class TimedEventInspection : IScriptInspection
    {
        private HashSet<int> handledEvents = new();
        private List<SmartEvent> timedEvents = new();
        private List<SmartAction> timedActions = new();
        public IEnumerable<InspectionResult> Inspect(SmartScriptBase script)
        {
            handledEvents.Clear();
            timedEvents.Clear();
            timedActions.Clear();
            
            foreach (var ev in script.Events)
            {
                if (ev.Id == SmartConstants.EventTriggerTimed)
                {
                    handledEvents.Add((int)ev.GetParameter(0).Value);
                    timedEvents.Add(ev);
                }

                foreach (var a in ev.Actions)
                {
                    if (a.Id == SmartConstants.ActionCreateTimed ||
                        a.Id == SmartConstants.ActionTriggerTimed || 
                        a.Id == SmartConstants.ActionTriggerRandomTimed ||
                        a.Id == SmartConstants.ActionRemoveTimed)
                        timedActions.Add(a);
                }
            }

            foreach (var e in timedEvents)
            {
                bool found = false;
                long id = e.GetParameter(0).Value;
                foreach (var a in timedActions)
                {
                    if (a.Id == SmartConstants.ActionTriggerRandomTimed)
                    {
                        if (id >= a.GetParameter(0).Value && id <= a.GetParameter(1).Value)
                        {
                            found = true;
                            break;
                        }
                    }
                    else
                    {
                        if (id == a.GetParameter(0).Value)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    yield return new InspectionResult()
                    {
                        Line = e.VirtualLineId,
                        Severity = DiagnosticSeverity.Info,
                        Message = "Timed event " + e.GetParameter(0).Value + " never triggered."
                    };
                }
            }
            
            foreach (var action in timedActions)
            {
                bool found = false;
                long id = -1;
                if (action.Id == SmartConstants.ActionTriggerRandomTimed)
                {
                    long start = action.GetParameter(0).Value;
                    long end = action.GetParameter(1).Value;
                    if (end >= start + 10)
                        end = start + 10;
                    found = true;
                    for (id = start; id < end; ++id)
                    {
                        if (!handledEvents.Contains((int) id))
                        {
                            found = false;
                            break;
                        }
                    }
                }
                else
                {
                    id = action.GetParameter(0).Value;
                    found = handledEvents.Contains((int) id);
                }

                if (!found && id != 0)
                {
                    yield return new InspectionResult()
                    {
                        Message = "Triggering non existing timed event (" + id + ").",
                        Severity = DiagnosticSeverity.Warning,
                        Line = action.VirtualLineId
                    };
                }
            }
        }
    }
}