using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    [UniqueProvider]
    public interface IEventAiInspectorService
    {
        IReadOnlyList<IInspectionResult> GenerateInspections(EventAiBase script);
    }
    
    [AutoRegister]
    public class EventAiInspectorService : IEventAiInspectorService
    {
        private readonly IList<IScriptInspection> scriptInspections = new List<IScriptInspection>();

        private readonly IList<IEventInspection> eventInspections = new List<IEventInspection>();
        private readonly Dictionary<uint, IList<IEventInspection>> perEventInspections = new();
        
        private readonly IList<IActionInspection> actionInspections = new List<IActionInspection>();
        private readonly Dictionary<uint, IList<IActionInspection>> perActionInspection = new();
        
        public EventAiInspectorService(IEventAiDataManager eventAiDataManager, ICurrentCoreVersion currentCoreVersion)
        {
            foreach (var ev in eventAiDataManager.GetAllData(EventOrAction.Event))
            {
                foreach (var inspection in GenerateRules(ev))
                    AddEventRule(inspection.Item1, inspection.Item2);
            }
            
            foreach (var ev in eventAiDataManager.GetAllData(EventOrAction.Action))
            {
                foreach (var inspection in GenerateRules(ev))
                    AddActionRule(inspection.Item1, inspection.Item2);
            }
            
            eventInspections.Add(new ParameterRangeInspection());
            eventInspections.Add(new IndentationInspection());
            eventInspections.Add(new TooManyActionsInspection(eventAiDataManager));
            
            actionInspections.Add(new ParameterRangeInspection());
            
            scriptInspections.Add(new DuplicateEventsInspection());
            
            eventInspections.Add(new UnusedParameterInspection());
            actionInspections.Add(new UnusedParameterInspection());
        }
        
        public IReadOnlyList<IInspectionResult> GenerateInspections(EventAiBase script)
        {
            var list = new List<IInspectionResult>();
            
            list.AddRange(GenerateInspectionsForScript(script));
            foreach (var ev in script.Events)
                list.AddRange(GenerateInspectionsForEvent(ev));

            return list;
        }
        
        private IEnumerable<IInspectionResult> GenerateInspectionsForScript(EventAiBase script)
        {
            foreach (var i in scriptInspections)
            {
                foreach (var inspection in i.Inspect(script))
                    yield return inspection;
            }
        }

        private IEnumerable<IInspectionResult> GenerateInspectionsForEvent(EventAiEvent ev)
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
        
        private IEnumerable<(uint, IEventActionInspection)> GenerateRules(EventActionGenericJsonData ev)
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
                    else if (type == "minmax")
                    {
                        if (prams.Count == 2)
                            yield return (ev.Id, new MinMaxInspection(ev, prams[0] - 1, prams[1] - 1));
                    }
                    else if (type == "maxmin")
                    {
                        if (prams.Count == 2)
                            yield return (ev.Id, new MinMaxInspection(ev, prams[1] - 1, prams[0] - 1));
                    }
                    else if (type == "percent")
                    {
                        if (prams.Count == 1)
                            yield return (ev.Id, new PercentValueInspection(ev, prams[0] - 1));
                    }
                }
            }
        }

        private void AddEventRule(uint eventId, IEventInspection inspection)
        {
            if (!perEventInspections.TryGetValue(eventId, out var insp))
            {
                insp = new List<IEventInspection>();
                perEventInspections[eventId] = insp;
            }
            insp.Add(inspection);
        }

        private void AddActionRule(uint actionId, IActionInspection inspection)
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