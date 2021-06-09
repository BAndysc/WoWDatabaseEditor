using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Exporter
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityCoreSmartScriptImporter : ISmartScriptImporter
    {
        private readonly ISmartFactory smartFactory;
        private readonly ISmartDataManager smartDataManager;
        private readonly IMessageBoxService messageBoxService;

        public TrinityCoreSmartScriptImporter(ISmartFactory smartFactory,
            ISmartDataManager smartDataManager,
            IMessageBoxService messageBoxService)
        {
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
            this.messageBoxService = messageBoxService;
        }
        private bool TryParseGlobalVariable(SmartScript script, ISmartScriptLine line)
        {
            if (line.EventType != SmartConstants.EventAiInitialize)
                return false;
            if (line.ActionType != SmartConstants.ActionNone)
                return false;
            if (!line.Comment.StartsWith("#define"))
                return false;
            
            Match match = Regex.Match(line.Comment, @"#define ([A-Za-z]+) (\d+) (.*?)(?: -- (.*?))?$", RegexOptions.IgnoreCase);
            if (!match.Success)
                return false;

            if (!Enum.TryParse(typeof(GlobalVariableType), match.Groups[1].Value, out var enm) || enm == null)
                return false;

            if (!long.TryParse(match.Groups[2].Value, out var key))
                return false;

            var variable = new GlobalVariable()
            {
                Name = match.Groups[3].Value,
                Comment = match.Groups.Count == 5 ? match.Groups[4].Value : null,
                Key = key,
                VariableType = (GlobalVariableType)enm
            };
            script.GlobalVariables.Add(variable);
            return true;
        }

        public void Import(SmartScript script, IList<ISmartScriptLine> lines, IList<IConditionLine> conditions, IList<IConditionLine> targetConditions)
        {
            int? entry = null;
            SmartScriptType? source = null;

            var conds = script.ParseConditions(conditions);
            SortedDictionary<long, SmartEvent> triggerIdToActionParent = new();
            SortedDictionary<long, SmartEvent> triggerIdToEvent = new();
            Dictionary<int, SmartEvent> linkToSmartEventParent = new();

            // find double links (multiple events linking to same event, this is not supported by design)
            var doubleLinks = lines
                .Where(line => line.Link > 0)
                .GroupBy(link => link.Link)
                .Where(pair => pair.Count() > 1)
                .Select(pair => pair.Key)
                .ToHashSet();
            
            Dictionary<int, int>? linkToTriggerTimedEventId = null;
            if (doubleLinks.Count > 0)
            {
                int nextFreeTriggerTimedEvent = lines.Where(e => e.Id == SmartConstants.EventTriggerTimed)
                    .Select(e => (int)e.EventParam1)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

                linkToTriggerTimedEventId = doubleLinks.Select(linkId => (linkId, nextFreeTriggerTimedEvent++))
                    .ToDictionary(pair => pair.linkId, pair => pair.Item2);
            }

            SmartEvent? lastEvent = null;
            foreach (ISmartScriptLine line in lines)
            {
                if (TryParseGlobalVariable(script, line))
                    continue;
                
                SmartEvent? currentEvent = null;
                
                if (!entry.HasValue)
                    entry = line.EntryOrGuid;
                else
                    Debug.Assert(entry.Value == line.EntryOrGuid);

                if (!source.HasValue)
                    source = (SmartScriptType) line.ScriptSourceType;
                else
                    Debug.Assert((int) source.Value == line.ScriptSourceType);

                if (source == SmartScriptType.TimedActionList && lastEvent != null && 
                    line.EventParam1 == 0 && line.EventParam2 == 0)
                    currentEvent = lastEvent;
                else if (!linkToSmartEventParent.TryGetValue(line.Id, out currentEvent))
                {
                    lastEvent = currentEvent = script.SafeEventFactory(line);

                    if (currentEvent != null)
                    {
                        if (currentEvent.Id == SmartConstants.EventTriggerTimed)
                            triggerIdToEvent[currentEvent.GetParameter(0).Value] = currentEvent;
                        else if (currentEvent.Id == SmartConstants.EventLink && doubleLinks.Contains(line.Id))
                        {
                            smartFactory.UpdateEvent(currentEvent, SmartConstants.EventTriggerTimed);
                            currentEvent.GetParameter(0).Value = linkToTriggerTimedEventId![line.Id];
                        }

                        if (conds.TryGetValue(line.Id, out var conditionList))
                        {
                            foreach (var c in conditionList)
                                currentEvent.Conditions.Add(c);
                        }

                        currentEvent.Parent = script;
                        script.Events.Add(currentEvent);
                    }
                    else
                        continue;
                }
                
                string comment = line.Comment.Contains(" // ") ? line.Comment.Substring(line.Comment.IndexOf(" // ") + 4).Trim() : "";

                if (!string.IsNullOrEmpty(comment) && line.ActionType == SmartConstants.ActionNone)
                {
                    line.ActionType = SmartConstants.ActionComment;
                }

                SmartAction? action = script.SafeActionFactory(line);
                
                if (action != null)
                {
                    var raw = smartDataManager.GetRawData(SmartType.SmartAction, line.ActionType);
                    if (raw.TargetIsSource)
                    {
                        script.SafeUpdateSource(action.Source, action.Target.Id);
                        for (int i = 0; i < action.Source.ParametersCount; ++i)
                            action.Source.GetParameter(i).Copy(action.Target.GetParameter(i));
                        smartFactory.UpdateTarget(action.Target, 0);
                    }
                    
                    if (comment != SmartConstants.CommentWait)
                        action.Comment = comment;
                    if (action.Id == SmartConstants.ActionCreateTimed && comment == SmartConstants.CommentWait)
                        triggerIdToActionParent[action.GetParameter(0).Value] = currentEvent;
                    currentEvent.AddAction(action);
                }
                
                if (line.Link != 0 && !doubleLinks.Contains(line.Link))
                {
                    linkToSmartEventParent[line.Link] = currentEvent;
                }
                else if (line.Link != 0)
                {
                    var actionCallLinkedAsTrigger = smartFactory.ActionFactory(SmartConstants.ActionCreateTimed,
                        smartFactory.SourceFactory(SmartConstants.ActionNone),
                        smartFactory.TargetFactory(SmartConstants.TargetNone));
                    actionCallLinkedAsTrigger.GetParameter(0).Value = linkToTriggerTimedEventId![line.Link];
                    currentEvent.AddAction(actionCallLinkedAsTrigger);
                }
            }

            var sortedTriggers = triggerIdToEvent.Keys.ToList();
            sortedTriggers.Reverse();
            foreach (long triggerId in sortedTriggers)
            {
                SmartEvent @event = triggerIdToEvent[triggerId];
                if (!triggerIdToActionParent.ContainsKey(triggerId))
                    continue;

                SmartEvent caller = triggerIdToActionParent[triggerId];

                int indexOfAction = -1;
                for (int i = 0; i < caller.Actions.Count; ++i)
                {
                    if (caller.Actions[i].Id == SmartConstants.ActionCreateTimed &&
                        caller.Actions[i].GetParameter(1).Value == caller.Actions[i].GetParameter(2).Value &&
                        caller.Actions[i].GetParameter(0).Value == triggerId)
                    {
                        indexOfAction = i;
                        break;
                    }
                }

                if (indexOfAction == -1)
                    continue;

                long waitTime = caller.Actions[indexOfAction].GetParameter(1).Value;
                if (indexOfAction > 0 && caller.Actions[indexOfAction - 1].Id == SmartConstants.ActionCreateTimed)
                    waitTime -= caller.Actions[indexOfAction - 1].GetParameter(1).Value;
                SmartAction waitAction = smartFactory.ActionFactory(SmartConstants.ActionWait,
                    smartFactory.SourceFactory(SmartConstants.SourceNone),
                    smartFactory.TargetFactory(SmartConstants.TargetNone));
                waitAction.GetParameter(0).Value = waitTime;

                caller.Actions.RemoveAt(indexOfAction);
                caller.InsertAction(waitAction, indexOfAction++);
                script.Events.Remove(@event);
                foreach (SmartAction a in @event.Actions)
                    caller.InsertAction(a, indexOfAction++);
            }
            

            if (doubleLinks.Count > 0)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Script modified")
                    .SetMainInstruction("Script with multiple links")
                    .SetContent(
                        "This script contains multiple events pointing to the same event (at least two events have the same `link` field). This is not supported by design in this editor. Therefore those links has been replaced with trigger_timed_event for you. The effect of the script should be the same")
                    .SetFooter("Nothing has been saved nowhere yet. This is just an note about loaded script.")
                    .WithOkButton(false)
                    .SetIcon(MessageBoxIcon.Warning)
                    .Build());
            }
        }
    }
}