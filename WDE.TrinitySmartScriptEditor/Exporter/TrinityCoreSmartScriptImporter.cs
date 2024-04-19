using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Utils;

namespace WDE.TrinitySmartScriptEditor.Exporter
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityCoreSmartScriptImporter : ISmartScriptImporter
    {
        private readonly ISmartFactory smartFactory;
        private readonly ISmartDataManager smartDataManager;
        private readonly IMessageBoxService messageBoxService;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IEditorFeatures editorFeatures;
        private readonly ISimpleConditionsImporter simpleConditionsImporter;

        public TrinityCoreSmartScriptImporter(ISmartFactory smartFactory,
            ISmartDataManager smartDataManager,
            IMessageBoxService messageBoxService,
            IDatabaseProvider databaseProvider,
            IEditorFeatures editorFeatures,
            ISimpleConditionsImporter simpleConditionsImporter)
        {
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
            this.messageBoxService = messageBoxService;
            this.databaseProvider = databaseProvider;
            this.editorFeatures = editorFeatures;
            this.simpleConditionsImporter = simpleConditionsImporter;
        }
        
        private bool TryParseGlobalVariable(SmartScript script, ISmartScriptLine line)
        {
            if (line.EventType != SmartConstants.EventAiInitialize)
                return false;
            if (line.ActionType != SmartConstants.ActionNone)
                return false;
            if (line.Comment.TryParseGlobalVariable(out var variable))
            {
                script.GlobalVariables.Add(variable);
                return true;
            }

            return false;
        }

        public Dictionary<int, List<SmartCondition>> ImportConditions(SmartScriptBase script, IReadOnlyList<IConditionLine> lines)
        {
            return simpleConditionsImporter.ImportConditions(script, lines);
        }

        public List<SmartCondition> ImportConditions(SmartScriptBase script, IReadOnlyList<ICondition> lines)
        {
            return simpleConditionsImporter.ImportConditions(script, lines);
        }

        public async Task Import(SmartScript script, bool doNotTouchIfPossible, IReadOnlyList<ISmartScriptLine> lines, IReadOnlyList<IConditionLine> conditions, IReadOnlyList<IConditionLine>? targetConditions)
        {
            int? entry = null;
            SmartScriptType? source = null;
            bool? shouldMergeUserLastAnswer = doNotTouchIfPossible ? false : null;

            var conds = ImportConditions(script, conditions);
            SortedDictionary<long, SmartEvent> startPathToActionParent = new();
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
            List<int> onTimedEventToRemove = new(); // timed event ids that we want to remove, because they belong to REPEAT meta action
            foreach (var line in lines)
            {
                if (TryParseGlobalVariable(script, line))
                    continue;

                SmartEvent? currentEvent = null;

                if (!entry.HasValue)
                    entry = line.EntryOrGuid;
                else
                    Debug.Assert(entry.Value == line.EntryOrGuid);

                if (!source.HasValue)
                    source = (SmartScriptType)line.ScriptSourceType;
                else
                    Debug.Assert((int)source.Value == line.ScriptSourceType);

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

                string comment = line.Comment.Contains(" // ")
                    ? line.Comment.Substring(line.Comment.IndexOf(" // ") + 4).Trim()
                    : "";

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
                        {
                            action.Source.GetParameter(i).Copy(action.Target.GetParameter(i));
                            action.Target.GetParameter(i).Value = 0;
                        }

                        smartFactory.UpdateTarget(action.Target, 0);
                    }

                    if (comment != SmartConstants.CommentWait &&
                        comment != SmartConstants.CommentInlineActionList &&
                        comment != SmartConstants.CommentInlineMovementActionList)
                        action.Comment = comment;

                    bool mergeList = action.Id == SmartConstants.ActionCallTimedActionList &&
                                     (comment == SmartConstants.CommentInlineActionList ||
                                      comment == SmartConstants.CommentInlineMovementActionList);

                    if (action.Id == SmartConstants.ActionCallTimedActionList && line.Link == 0 && !mergeList)
                    {
                        var shouldMerge =
                            await TryMergeTimedActionList(action.GetParameter(0).Value, shouldMergeUserLastAnswer);
                        if (!shouldMerge.usedMultiple)
                        {
                            mergeList = shouldMerge.userAnswer ?? false;
                            shouldMergeUserLastAnswer = shouldMerge.userAnswer;
                        }
                    }

                    if (mergeList)
                    {
                        var timedScript = await databaseProvider.GetScriptForAsync(0, (int)action.GetParameter(0).Value,
                            SmartScriptType.TimedActionList);
                        var beginInlineAction = smartFactory.ActionFactory(SmartConstants.ActionBeginInlineActionList,
                            smartFactory.SourceFactory(SmartConstants.SourceSelf),
                            smartFactory.TargetFactory(SmartConstants.TargetSelf));
                        beginInlineAction.GetParameter(0).Value = action.GetParameter(1).Value;
                        beginInlineAction.GetParameter(1).Value = action.GetParameter(2).Value;
                        var pseudoScript = new SmartScript(
                            new SmartScriptSolutionItem((int)action.GetParameter(0).Value,
                                SmartScriptType.TimedActionList),
                            smartFactory, smartDataManager, messageBoxService, editorFeatures, this);
                        await Import(pseudoScript, doNotTouchIfPossible, timedScript.ToList(), new List<IConditionLine>(),
                            new List<IConditionLine>());
                        currentEvent.AddAction(beginInlineAction);
                        foreach (var e in pseudoScript.Events)
                        {
                            if (e.GetParameter(0).Value > 0)
                            {
                                var afterAction = smartFactory.ActionFactory(SmartConstants.ActionAfter, null, null);
                                afterAction.GetParameter(0).Value = e.GetParameter(0).Value;
                                afterAction.GetParameter(1).Value = e.GetParameter(0).Value == e.GetParameter(1).Value
                                    ? 0
                                    : e.GetParameter(1).Value;
                                currentEvent.AddAction(afterAction);
                            }

                            foreach (var a in e.Actions)
                            {
                                if (a.Comment == SmartConstants.CommentInlineRepeatActionList)
                                {
                                    onTimedEventToRemove.Add((int)a.GetParameter(0).Value);
                                    var actualAction = smartFactory.ActionFactory(SmartConstants.ActionRepeatTimedActionList, null, null);
                                    currentEvent.AddAction(actualAction);
                                }
                                else
                                    currentEvent.AddAction(a);
                            }
                        }
                    }
                    else
                    {
                        if (action.Id == SmartConstants.ActionCreateTimed && comment == SmartConstants.CommentWait)
                            triggerIdToActionParent[action.GetParameter(0).Value] = currentEvent;
                        currentEvent.AddAction(action);
                    }
                }

                if (source == SmartScriptType.TimedActionList &&
                    (line.EventFlags & SmartConstants.EventFlagActionListWaits) ==
                    SmartConstants.EventFlagActionListWaits)
                {
                    var awaitAction = smartFactory.ActionFactory(SmartConstants.ActionAwaitTimedList,
                        smartFactory.SourceFactory(SmartConstants.SourceNone),
                        smartFactory.TargetFactory(SmartConstants.TargetNone));
                    currentEvent.AddAction(awaitAction);
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

            foreach (var e in script.Events)
            {
                if (e.Actions.Count > 0)
                {
                    var a = e.Actions[^1];
                    if (a.Id is SmartConstants.ActionStartWaypointsPath)
                        startPathToActionParent[a.GetParameter(1).Value] = e; // path id
                    else if (a.Id is SmartConstants.ActionMovePoint)
                        startPathToActionParent[a.GetParameter(0).Value] = e; // point id
                }
            }
            
            for (var index = script.Events.Count - 1; index >= 0; index--)
            {
                var e = script.Events[index];
                
                if (e.Actions.Count == 0 || e.Actions[0].Id != SmartConstants.ActionBeginInlineActionList)
                    continue;

                if (!startPathToActionParent.TryGetValue(e.GetParameter(1).Value, out var startPathEvent))
                    continue;
                
                if ((e.Id == SmartConstants.EventWaypointsEnded && e.GetParameter(0).Value == 0) ||
                    (e.Id == SmartConstants.EventMovementInform && 
                     e.GetParameter(0).Value == SmartConstants.MovementTypePointMotionType &&
                     e.GetParameter(1).Value != 0))
                {
                    script.Events.Remove(e);
                    startPathEvent.AddAction(smartFactory.ActionFactory(SmartConstants.ActionAfterMovement, null, null));
                    for (int i = 1; i < e.Actions.Count; ++i)
                        startPathEvent.AddAction(e.Actions[i]);   
                }
            }

            if (onTimedEventToRemove.Count > 0)
            {
                for (var index = script.Events.Count - 1; index >= 0; index--)
                {
                    var e = script.Events[index];
                    if (e.Id != SmartConstants.EventTriggerTimed)
                        continue;
                    
                    if (e.Actions.Count != 1)
                        continue;

                    if (e.Actions[0].Id != SmartConstants.ActionCallTimedActionList)
                        continue;

                    if (!onTimedEventToRemove.Contains((int)e.GetParameter(0).Value))
                        continue;
                    
                    script.Events.RemoveAt(index);
                }
            }

            if (doubleLinks.Count > 0)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Script modified")
                    .SetMainInstruction("Script with multiple links")
                    .SetContent(
                        "This script contains multiple events pointing to the same event (at least two events have the same `link` field). This is not supported by design in this editor. Therefore those links has been replaced with trigger_timed_event for you. The effect of the script should be the same")
                    .SetFooter("Nothing has been saved nowhere yet. This is just an note about loaded script.")
                    .WithOkButton(false)
                    .SetIcon(MessageBoxIcon.Warning)
                    .Build());
            }
        }

        private async Task<(bool usedMultiple, bool? userAnswer)> TryMergeTimedActionList(long timedActionList, bool? mergeIfPossible)
        {
            if (mergeIfPossible is false)
                return (true, mergeIfPossible);
            
            var lines = await databaseProvider.GetLinesCallingSmartTimedActionList((int)timedActionList);
            if (lines.Count > 1)
                return (true, mergeIfPossible);

            if (mergeIfPossible is true)
                return (false, mergeIfPossible);
            
            return (false, await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Merge timed action list")
                .SetMainInstruction("Do you want to merge timed action list into main script?")
                .SetContent(
                    "The editor has detected calling timed action list, that is called only once in whole database.\n\nThat means this timed action list can be merged into the main script.\nThe generated query will be the same as now.\n\nDo you want to merge it?")
                .WithYesButton(true)
                .WithNoButton(false)
                .SetIcon(MessageBoxIcon.Information)
                .Build()));
        }
    }
}