using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Exporter
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityCoreSmartScriptExporter : ISmartScriptExporter
    {
        private readonly ISmartFactory smartFactory;
        private readonly ISmartDataManager smartDataManager;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly ISolutionItemNameRegistry nameRegistry;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IConditionQueryGenerator conditionQueryGenerator;

        public TrinityCoreSmartScriptExporter(ISmartFactory smartFactory,
            ISmartDataManager smartDataManager,
            ICurrentCoreVersion currentCoreVersion,
            ISolutionItemNameRegistry nameRegistry,
            IDatabaseProvider databaseProvider,
            IMessageBoxService messageBoxService,
            IConditionQueryGenerator conditionQueryGenerator)
        {
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
            this.currentCoreVersion = currentCoreVersion;
            this.nameRegistry = nameRegistry;
            this.databaseProvider = databaseProvider;
            this.messageBoxService = messageBoxService;
            this.conditionQueryGenerator = conditionQueryGenerator;
        }
        
        public (ISmartScriptLine[], IConditionLine[]) ToDatabaseCompatibleSmartScript(SmartScript script)
        {
            if (script.Events.Count == 0)
                return (new ISmartScriptLine[0], null);

            var eventId = 0;
            var lines = new List<ISmartScriptLine>();
            var conditions = new List<IConditionLine>();
            var previousWasWait = false;
            long nextTriggerId = script.Events.Where(e => e.Id == SmartConstants.EventTriggerTimed)
                .Select(e => e.GetParameter(0).Value)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var usedTimedActionLists = script.Events
                .SelectMany(e => e.Actions)
                .Where(a => a.Id == SmartConstants.ActionCallTimedActionList ||
                            a.Id == SmartConstants.ActionCallRandomTimedActionList ||
                            a.Id == SmartConstants.ActionCallRandomRangeTimedActionList)
                .SelectMany(a =>
                {
                    if (a.Id == SmartConstants.ActionCallRandomTimedActionList)
                        return new int[]
                        {
                            (int)a.GetParameter(0).Value,
                            (int)a.GetParameter(1).Value,
                            (int)a.GetParameter(2).Value,
                            (int)a.GetParameter(3).Value,
                            (int)a.GetParameter(4).Value,
                            (int)a.GetParameter(5).Value,
                        };
                    if (a.Id == SmartConstants.ActionCallRandomRangeTimedActionList &&
                        a.GetParameter(1).Value - a.GetParameter(0).Value < 20)
                        return Enumerable.Range((int)a.GetParameter(0).Value, (int)(a.GetParameter(1).Value - a.GetParameter(0).Value + 1));
                    return new int[] { (int)a.GetParameter(0).Value };
                })
                .Where(id => id != 0)
                .ToHashSet();

            int firstUnusedActionList = Math.Abs(script.EntryOrGuid) * 100 -1;
            int GetNextUnusedTimedActionList()
            {
                do
                {
                    firstUnusedActionList++;
                } while (usedTimedActionLists.Contains(firstUnusedActionList));

                usedTimedActionLists.Add(firstUnusedActionList);
                return firstUnusedActionList;
            }
            
            foreach (var gv in script.GlobalVariables)
                lines.Add(gv.ToMetaSmartScriptLine(script.EntryOrGuid, script.SourceType, eventId++));    

            if (script.SourceType == SmartScriptType.TimedActionList)
            {
                foreach (SmartEvent e in script.Events)
                {
                    for (var index = 0; index < e.Actions.Count; ++index)
                    {
                        SmartEvent eventToSerialize = index == 0 ? e.ShallowCopy() : smartFactory.EventFactory(SmartConstants.EventUpdateInCombat);

                        SmartAction actualAction = e.Actions[index].Copy();
                        AdjustCoreCompatibleAction(actualAction);
                        
                        eventToSerialize.Parent = script;
                        eventToSerialize.Actions.Add(actualAction);
                        
                        var serialized = eventToSerialize.ToSmartScriptLines(script.EntryOrGuid, script.SourceType, eventId++, false, 0);

                        if (serialized.Length != 1)
                            throw new InvalidOperationException();

                        lines.Add(serialized[0]);
                    }
                }
            }
            else
            {
                SmartEvent originalEvent;
                List<SmartEvent> additionalEvents = new();

                void FlushLines(SmartEvent? eventForConditions)
                {
                    if (eventForConditions != null)
                    {
                        var serializedConditions = eventForConditions.ToConditionLines(SmartConstants.ConditionSourceSmartScript, script.EntryOrGuid, script.SourceType, eventId);
                        if (serializedConditions != null)
                            conditions.AddRange(serializedConditions);
                    }
                    
                    foreach (var toSerialize in new SmartEvent[] {originalEvent}.Concat(additionalEvents))
                    {
                        if (toSerialize.Actions.Count == 0)
                            continue;
                        var serialized = toSerialize.ToSmartScriptLines(script.EntryOrGuid, script.SourceType, eventId, true);
                        eventId += serialized.Length;
                        lines.AddRange(serialized);
                    }
                    additionalEvents.ForEach(d => d.Actions.Clear());
                    originalEvent.Actions.Clear();
                }
                
                foreach (SmartEvent e in script.Events)
                {
                    if (e.Actions.Count == 0)
                        continue;

                    originalEvent = e.ShallowCopy();
                    originalEvent.Parent = script;
                    additionalEvents.Clear();
                    SmartEvent lastEvent = originalEvent;
                    
                    long accumulatedWaits = 0;
                    for (var index = 0; index < e.Actions.Count; ++index)
                    {
                        if (previousWasWait)
                        {
                            var eventTimed = smartFactory.EventFactory(SmartConstants.EventTriggerTimed);
                            eventTimed.Parent = script;
                            eventTimed.GetParameter(0).Value = nextTriggerId++;
                            additionalEvents.Add(eventTimed);
                            lastEvent = eventTimed;
                        }

                        if (e.Actions[index].Id == SmartConstants.ActionBeginInlineActionList ||
                            e.Actions[index].Id == SmartConstants.ActionAfter)
                        {
                            var timedActionListId = GetNextUnusedTimedActionList();
                            SmartAction callTimedActionList = smartFactory.ActionFactory(SmartConstants.ActionCallTimedActionList,
                                smartFactory.SourceFactory(SmartConstants.SourceSelf),
                                smartFactory.TargetFactory(SmartConstants.TargetSelf));
                            callTimedActionList.GetParameter(0).Value = timedActionListId;
                            if (e.Actions[index].Id == SmartConstants.ActionBeginInlineActionList)
                            {
                                callTimedActionList.GetParameter(1).Value = e.Actions[index].GetParameter(0).Value;
                                callTimedActionList.GetParameter(2).Value = e.Actions[index].GetParameter(1).Value;
                                index++;
                            }
                            callTimedActionList.Comment = SmartConstants.CommentInlineActionList;
                            lastEvent.AddAction(callTimedActionList);

                            FlushLines(e);
                            
                            long afterTimeMin = 0;
                            long afterTimeMax = 0;
                            int timedEventId = 0;
                            for (; index < e.Actions.Count; ++index)
                            {
                                if (e.Actions[index].Id == SmartConstants.ActionAfter || e.Actions[index].Id == SmartConstants.ActionWait)
                                {
                                    afterTimeMin += e.Actions[index].GetParameter(0).Value;
                                    afterTimeMax += e.Actions[index].GetParameter(1).Value;
                                    if (e.Actions[index].GetParameter(1).Value == 0)
                                        afterTimeMax += e.Actions[index].GetParameter(0).Value;
                                }
                                else if (e.Actions[index].Id == SmartConstants.ActionAfterMovement && index > 0)
                                {
                                    afterTimeMin = 0;
                                    afterTimeMax = 0;
                                    var pathId = e.Actions[index - 1].GetParameter(1).Value;
                                    timedActionListId = GetNextUnusedTimedActionList();

                                    var eventFinishedMovement =
                                        smartFactory.EventFactory(SmartConstants.EventWaypointsEnded);
                                    eventFinishedMovement.Parent = script;
                                    eventFinishedMovement.GetParameter(1).Value = pathId;
                                    
                                    var callAnotherTimedActionList = smartFactory.ActionFactory(SmartConstants.ActionCallTimedActionList,
                                        smartFactory.SourceFactory(SmartConstants.SourceSelf),
                                        smartFactory.TargetFactory(SmartConstants.TargetSelf));
                                    callAnotherTimedActionList.GetParameter(0).Value = timedActionListId;
                                    callAnotherTimedActionList.Comment = SmartConstants.CommentInlineMovementActionList;
                                    
                                    eventFinishedMovement.AddAction(callAnotherTimedActionList);
                                    additionalEvents.Add(eventFinishedMovement);
                                    FlushLines(null);
                                }
                                else
                                {
                                    SmartEvent after = smartFactory.EventFactory(SmartConstants.EventUpdateInCombat);
                                    after.GetParameter(0).Value = afterTimeMin;
                                    after.GetParameter(1).Value = afterTimeMax;
                                    SmartAction actualAction = e.Actions[index].Copy();
                                    AdjustCoreCompatibleAction(actualAction);
                        
                                    after.Parent = new SmartScript(new SmartScriptSolutionItem(timedActionListId, SmartScriptType.TimedActionList), smartFactory, smartDataManager, messageBoxService);
                                    after.AddAction(actualAction);
                        
                                    var serialized = after.ToSmartScriptLines(timedActionListId, SmartScriptType.TimedActionList, timedEventId++, false, 0);

                                    if (serialized.Length != 1)
                                        throw new InvalidOperationException();

                                    lines.Add(serialized[0]);
                                    afterTimeMin = 0;
                                    afterTimeMax = 0;
                                }
                            }
                        }
                        else if (e.Actions[index].Id == SmartConstants.ActionWait)
                        {
                            accumulatedWaits += e.Actions[index].GetParameter(0).Value;

                            if (index == e.Actions.Count - 1 || e.Actions[index + 1].Id == SmartConstants.ActionWait)
                                continue;

                            SmartAction createTimedAction = smartFactory.ActionFactory(SmartConstants.ActionCreateTimed,
                                smartFactory.SourceFactory(SmartConstants.SourceNone),
                                smartFactory.TargetFactory(SmartConstants.TargetNone));
                            createTimedAction.GetParameter(0).Value = nextTriggerId;
                            createTimedAction.GetParameter(1).Value = accumulatedWaits;
                            createTimedAction.GetParameter(2).Value = accumulatedWaits;
                            createTimedAction.Comment = SmartConstants.CommentWait;
                            previousWasWait = true;
                            
                            originalEvent.AddAction(createTimedAction);
                        }
                        else
                        {
                            previousWasWait = false;
                            SmartAction actualAction = e.Actions[index].Copy();
                            AdjustCoreCompatibleAction(actualAction);
                            lastEvent.AddAction(actualAction);
                        }
                    }

                    if (originalEvent.Actions.Count == 0)
                        continue;

                    FlushLines(e);
                }
            }
            
            return (lines.ToArray(), conditions.ToArray());
        }

        private void AdjustCoreCompatibleAction(SmartAction action)
        {
            if (action.Id == SmartConstants.ActionComment)
            {
                smartFactory.UpdateAction(action, SmartConstants.ActionNone);
                smartFactory.UpdateSource(action.Source, SmartConstants.ActionNone);
                smartFactory.UpdateTarget(action.Target, SmartConstants.ActionNone);
            }
            
            var actionData = smartDataManager.GetRawData(SmartType.SmartAction, action.Id);

            if (actionData.ImplicitSource != null)
                smartFactory.UpdateSource(action.Source,
                    smartDataManager.GetDataByName(SmartType.SmartSource, actionData.ImplicitSource).Id);

            if (actionData.TargetIsSource)
            {
                smartFactory.UpdateTarget(action.Target, action.Source.Id);
                for (int i = 0; i < action.Target.ParametersCount; ++i)
                    action.Target.GetParameter(i).Copy(action.Source.GetParameter(i));

                // do not reset source, it doesn't matter, but at least correct comment will be generated
                // smartFactory.UpdateSource(actualAction.Source, 0);
            }

            if (actionData.SourceStoreInAction)
            {
                action.GetParameter(2).Value = action.Source.Id;
                action.GetParameter(3).Value = action.Source.GetParameter(0).Value;
                action.GetParameter(4).Value = action.Source.GetParameter(1).Value;
                action.GetParameter(5).Value = action.Source.GetParameter(2).Value;
            }
        }

        public string GenerateSql(ISmartScriptSolutionItem item, SmartScript script)
        {
            return new ExporterHelper(script, databaseProvider, item, this, currentCoreVersion, nameRegistry, conditionQueryGenerator).GetSql().QueryString;
        }
    }
}