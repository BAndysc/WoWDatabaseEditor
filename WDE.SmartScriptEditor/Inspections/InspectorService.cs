using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    [UniqueProvider]
    public interface ISmartScriptInspectorService
    {
        IReadOnlyList<IInspectionResult> GenerateInspections(SmartScriptBase script);
    }
    
    [AutoRegister]
    public class SmartScriptInspectorService : ISmartScriptInspectorService
    {
        private readonly IList<IScriptInspection> scriptInspections = new List<IScriptInspection>();

        private readonly IList<IEventInspection> eventInspections = new List<IEventInspection>();
        private readonly Dictionary<int, IList<IEventInspection>> perEventInspections = new();
        
        private readonly IList<IActionInspection> actionInspections = new List<IActionInspection>();
        private readonly Dictionary<int, IList<IActionInspection>> perActionInspection = new();
        
        private readonly Dictionary<int, IList<ITargetInspection>> perSourceInspection = new();

        public SmartScriptInspectorService(ISmartDataManager smartDataManager, ICurrentCoreVersion currentCoreVersion)
        {
            foreach (var ev in smartDataManager.GetAllData(SmartType.SmartEvent))
            {
                foreach (var inspection in GenerateRules(ev))
                    AddEventRule(inspection.Item1, inspection.Item2);
            }
            
            foreach (var ev in smartDataManager.GetAllData(SmartType.SmartAction))
            {
                foreach (var inspection in GenerateRules(ev))
                    AddActionRule(inspection.Item1, inspection.Item2);
            }
            
            foreach (var ev in smartDataManager.GetAllData(SmartType.SmartTarget))
            {
                foreach (var inspection in GenerateRules(ev))
                    AddTargetRule(inspection.Item1, inspection.Item2);
                
                if (ev.IsInvoker)
                    AddTargetRule(ev.Id, new TargetRequiresInvoker());
            }

            AddEventRule(6, new NoWaitInDeathEventInspection(smartDataManager));
            
            eventInspections.Add(new ParameterRangeInspection());
            eventInspections.Add(new NoActionInspection());
            eventInspections.Add(new NoActionInvokerAfterWaitInspection());
            eventInspections.Add(new AwaitAfterAsyncInspection(smartDataManager));
            eventInspections.Add(new IndentationInspection());
            eventInspections.Add(new BeginInlineActionListOnlyOnceInspection());
            eventInspections.Add(new MovementAwaitOnlyAfterMovement());
            
            actionInspections.Add(new ParameterRangeInspection());
            if (!currentCoreVersion.Current.SmartScriptFeatures.SupportsCreatingTimedEventsInsideTimedEvents)
                actionInspections.Add(new NoTimedCallsInTimedEventInspection());
            
            scriptInspections.Add(new WaitInCombatEventInspection(smartDataManager));
            scriptInspections.Add(new TimedEventInspection());
            scriptInspections.Add(new DuplicateEventsInspection());
            scriptInspections.Add(new TimedActionListAfter0EventsInspection());
            
            eventInspections.Add(new UnusedParameterInspection());
            actionInspections.Add(new UnusedParameterInspection());
        }
        
        public IReadOnlyList<IInspectionResult> GenerateInspections(SmartScriptBase script)
        {
            var list = new List<IInspectionResult>();
            
            list.AddRange(GenerateInspectionsForScript(script));
            foreach (var ev in script.Events)
                list.AddRange(GenerateInspectionsForEvent(ev));

            return list;
        }
        
        private IEnumerable<IInspectionResult> GenerateInspectionsForScript(SmartScriptBase script)
        {
            foreach (var i in scriptInspections)
            {
                foreach (var inspection in i.Inspect(script))
                    yield return inspection;
            }
        }

        private IEnumerable<IInspectionResult> GenerateInspectionsForEvent(SmartEvent ev)
        {
            foreach (var i in eventInspections)
            {
                var result = i.Inspect(ev);
                if (result == null)
                    continue;

                yield return result;
            }

            foreach (var i in actionInspections)
            {
                foreach (var action in ev.Actions)
                {
                    var result = i.Inspect(action);
                    if (result == null)
                        continue;

                    yield return result;
                }
            }

            foreach (var action in ev.Actions)
            {
                if (perSourceInspection.TryGetValue(action.Source.Id, out var sourceInspections))
                {
                    foreach (var i in sourceInspections)
                    {
                        var result = i.Inspect(action.Source);
                        if (result == null)
                            continue;

                        yield return result;
                    }
                }

                if (perSourceInspection.TryGetValue(action.Target.Id, out var targetInspections))
                {
                    foreach (var i in targetInspections)
                    {
                        var result = i.Inspect(action.Target);
                        if (result == null)
                            continue;

                        yield return result;
                    }
                }
                
                if (!perActionInspection.TryGetValue(action.Id, out var actionInspections))
                    continue;
                
                foreach (var i in actionInspections)
                {
                    var result = i.Inspect(action);
                    if (result == null)
                        continue;

                    yield return result;
                }
            }
            
            if (!perEventInspections.TryGetValue(ev.Id, out var insp))
                yield break;

            foreach (var i in insp)
            {
                var result = i.Inspect(ev);
                if (result == null)
                    continue;

                yield return result;
            }
        }
        
        private IEnumerable<(int, IEventActionInspection)> GenerateRules(SmartGenericJsonData ev)
        {
            if (ev.Rules != null)
            {
                foreach (var rule in ev.Rules)
                {
                    yield return (ev.Id, new JsonConditionInspection(rule));
                }
            }

            if (ev.Parameters != null)
            {
                for (int i = 0; i < ev.Parameters.Count; ++i)
                {
                    if (ev.Parameters[i].Required)
                        yield return (ev.Id, new NonZeroInspection(ev, i));
                    
                    if (ev.Parameters[i].Type == "PercentageParameter")
                        yield return (ev.Id, new PercentValueInspection(ev, i));
                }
            }

            if (ev.BuiltinRules != null)
            {
                foreach (var builtinRule in ev.BuiltinRules)
                {
                    int brace = builtinRule.IndexOf("(");
                    if (brace == -1 || builtinRule.Length == 0 || builtinRule[^1] != ')')
                        continue;

                    string type = builtinRule.Substring(0, brace);
                    List<int> prams = builtinRule
                        .Substring(brace + 1, builtinRule.Length - brace - 2)
                        .Split(',')
                        .Select(int.Parse).ToList();

                    if (type == "nonzero")
                    {
                        if (prams.Count == 1)
                            yield return (ev.Id, new NonZeroInspection(ev, prams[0] - 1));
                    }
                    else if (type == "is_repeat")
                    {
                        if (prams.Count == 1)
                            yield return (ev.Id, new RepeatInspection(ev, prams[0] - 1));
                    }
                    else if (type == "minmax")
                    {
                        if (prams.Count == 2)
                            yield return (ev.Id, new MinMaxInspection(ev, prams[0] - 1, prams[1] - 1));
                    }
                    else if (type == "percent")
                    {
                        if (prams.Count == 1)
                            yield return (ev.Id, new PercentValueInspection(ev, prams[0] - 1));
                    }
                }
            }
        }

        private void AddEventRule(int eventId, IEventInspection inspection)
        {
            if (!perEventInspections.TryGetValue(eventId, out var insp))
            {
                insp = new List<IEventInspection>();
                perEventInspections[eventId] = insp;
            }
            insp.Add(inspection);
        }

        private void AddTargetRule(int targetId, ITargetInspection inspection)
        {
            if (!perSourceInspection.TryGetValue(targetId, out var insp))
            {
                insp = new List<ITargetInspection>();
                perSourceInspection[targetId] = insp;
            }
            insp.Add(inspection);
        }

        private void AddActionRule(int actionId, IActionInspection inspection)
        {
            if (!perActionInspection.TryGetValue(actionId, out var insp))
            {
                insp = new List<IActionInspection>();
                perActionInspection[actionId] = insp;
            }
            insp.Add(inspection);
        }
    }
}