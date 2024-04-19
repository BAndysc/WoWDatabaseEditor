using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Models.Helpers;
using WDE.SqlQueryGenerator;

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
        private readonly ISmartScriptImporter importer;
        private readonly IEditorFeatures editorFeatures;
        private readonly IConditionQueryGenerator conditionQueryGenerator;

        public TrinityCoreSmartScriptExporter(ISmartFactory smartFactory,
            ISmartDataManager smartDataManager,
            ICurrentCoreVersion currentCoreVersion,
            ISolutionItemNameRegistry nameRegistry,
            IDatabaseProvider databaseProvider,
            IMessageBoxService messageBoxService,
            ISmartScriptImporter importer,
            IEditorFeatures editorFeatures,
            IConditionQueryGenerator conditionQueryGenerator)
        {
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
            this.currentCoreVersion = currentCoreVersion;
            this.nameRegistry = nameRegistry;
            this.databaseProvider = databaseProvider;
            this.messageBoxService = messageBoxService;
            this.importer = importer;
            this.editorFeatures = editorFeatures;
            this.conditionQueryGenerator = conditionQueryGenerator;
        }

        public IReadOnlyList<ICondition> ToDatabaseCompatibleConditions(SmartScript script, SmartEvent @event)
        {
            return ToDatabaseCompatibleConditions(script, @event, 0);
        }

        public IReadOnlyList<IConditionLine> ToDatabaseCompatibleConditions(SmartScript script, SmartEvent e, int id)
        {
            var lines = new List<AbstractConditionLine>();
            var elseGroup = 0;

            for (var index = 0; index < e.Conditions.Count; index++)
            {
                SmartCondition c = e.Conditions[index];
                if (c.Id == SmartConstants.ConditionOr)
                {
                    elseGroup++;
                    continue;
                }
                lines.Add(new AbstractConditionLine()
                {
                    SourceType = SmartConstants.ConditionSourceSmartScript,
                    SourceGroup = id + 1,
                    SourceEntry = script.EntryOrGuid,
                    SourceId = (int)script.SourceType,
                    ElseGroup = elseGroup,
                    ConditionType = c.Id,
                    ConditionTarget = (byte)c.ConditionTarget.Value,
                    ConditionValue1 = (int)c.GetParameter(0).Value,
                    ConditionValue2 = (int)c.GetParameter(1).Value,
                    ConditionValue3 = (int)c.GetParameter(2).Value,
                    ConditionStringValue1 = c.GetStringValueOrDefault(0) ?? "",
                    NegativeCondition = (int)c.Inverted.Value,
                    Comment = c.Readable.RemoveTags()
                });
            }
            return lines.ToArray();
        }

        public (ISmartScriptLine[], IConditionLine[]) ToDatabaseCompatibleSmartScript(SmartScript script)
        {
            if (script.Events.Count == 0)
                return (new ISmartScriptLine[0], new IConditionLine[0]);

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
                        SmartEvent eventToSerialize = index == 0 ? e.ShallowCopy() : smartFactory.EventFactory(null, SmartConstants.EventUpdateInCombat);
                        SmartAction actualAction = e.Actions[index].Copy();

                        // next is await
                        if (index + 1 < e.Actions.Count && e.Actions[index + 1].Id == SmartConstants.ActionAwaitTimedList)
                        {
                            index++;
                            eventToSerialize.Flags.Value |= SmartConstants.EventFlagActionListWaits;
                        }

                        AdjustCoreCompatibleAction(actualAction);
                        
                        eventToSerialize.Parent = script;
                        eventToSerialize.Actions.Add(actualAction);
                        
                        var serialized = eventToSerialize.ToSmartScriptLines(null, script.EntryOrGuid, script.SourceType, eventId++, false, 0);

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
                        var serializedConditions = ToDatabaseCompatibleConditions(script, eventForConditions, eventId);
                        if (serializedConditions != null)
                            conditions.AddRange(serializedConditions);
                    }
                    
                    foreach (var toSerialize in new SmartEvent[] {originalEvent}.Concat(additionalEvents))
                    {
                        if (toSerialize.Actions.Count == 0)
                            continue;
                        var serialized = toSerialize.ToSmartScriptLines(null, script.EntryOrGuid, script.SourceType, eventId, true);
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
                    int? firstInlineActionListId = null;
                    int? firstInlineActionListUpdateType = null;
                    bool? firstInlineActionListAllowOverride = null;
                    int? currentInlineActionListId = null;
                    for (var index = 0; index < e.Actions.Count; ++index)
                    {
                        if (previousWasWait)
                        {
                            var eventTimed = smartFactory.EventFactory(null, SmartConstants.EventTriggerTimed);
                            eventTimed.Parent = script;
                            eventTimed.GetParameter(0).Value = nextTriggerId++;
                            additionalEvents.Add(eventTimed);
                            lastEvent = eventTimed;
                        }

                        if (e.Actions[index].Id == SmartConstants.ActionBeginInlineActionList ||
                            e.Actions[index].Id == SmartConstants.ActionAfter)
                        {
                            firstInlineActionListId = currentInlineActionListId = GetNextUnusedTimedActionList();
                            SmartAction callTimedActionList = smartFactory.ActionFactory(SmartConstants.ActionCallTimedActionList,
                                smartFactory.SourceFactory(SmartConstants.SourceSelf),
                                smartFactory.TargetFactory(SmartConstants.TargetSelf));
                            callTimedActionList.GetParameter(0).Value = currentInlineActionListId.Value;
                            if (e.Actions[index].Id == SmartConstants.ActionBeginInlineActionList)
                            {
                                firstInlineActionListUpdateType = (int)e.Actions[index].GetParameter(0).Value;
                                firstInlineActionListAllowOverride = e.Actions[index].GetParameter(1).Value != 0;
                                callTimedActionList.GetParameter(1).Value = firstInlineActionListUpdateType.Value;
                                callTimedActionList.GetParameter(2).Value = firstInlineActionListAllowOverride.Value ? 1 : 0;
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
                                    var previousActionType = e.Actions[index - 1].Id;
                                    Debug.Assert(previousActionType is SmartConstants.ActionStartWaypointsPath or SmartConstants.ActionMovePoint);
                                    afterTimeMin = 0;
                                    afterTimeMax = 0;

                                    SmartEvent eventFinishedMovement;
                                    if (previousActionType == SmartConstants.ActionStartWaypointsPath)
                                    {
                                        var pathId = e.Actions[index - 1].GetParameter(1).Value;
                                        eventFinishedMovement = smartFactory.EventFactory(null, SmartConstants.EventWaypointsEnded);
                                        eventFinishedMovement.GetParameter(1).Value = pathId;
                                    }
                                    else if (previousActionType == SmartConstants.ActionMovePoint)
                                    {
                                        var pointId = e.Actions[index - 1].GetParameter(0).Value;
                                        eventFinishedMovement = smartFactory.EventFactory(null, SmartConstants.EventMovementInform);
                                        eventFinishedMovement.GetParameter(0).Value = SmartConstants.MovementTypePointMotionType;
                                        eventFinishedMovement.GetParameter(1).Value = pointId;
                                    }
                                    else
                                        throw new Exception("Invalid previosu action type");

                                    eventFinishedMovement.Parent = script;
                                    
                                    currentInlineActionListId = GetNextUnusedTimedActionList();
                                    var callAnotherTimedActionList = smartFactory.ActionFactory(SmartConstants.ActionCallTimedActionList,
                                        smartFactory.SourceFactory(SmartConstants.SourceSelf),
                                        smartFactory.TargetFactory(SmartConstants.TargetSelf));
                                    callAnotherTimedActionList.GetParameter(0).Value = currentInlineActionListId.Value;
                                    callAnotherTimedActionList.Comment = SmartConstants.CommentInlineMovementActionList;
                                    
                                    eventFinishedMovement.AddAction(callAnotherTimedActionList);
                                    additionalEvents.Add(eventFinishedMovement);
                                    FlushLines(null);
                                }
                                else
                                {
                                    SmartEvent after = smartFactory.EventFactory(null, SmartConstants.EventUpdateInCombat);
                                    after.GetParameter(0).Value = afterTimeMin;
                                    after.GetParameter(1).Value = afterTimeMax;
                                    SmartAction actualAction = e.Actions[index].Copy();

                                    if (index + 1 < e.Actions.Count &&
                                        e.Actions[index + 1].Id == SmartConstants.ActionAwaitTimedList)
                                    {
                                        index++;
                                        after.Flags.Value |= SmartConstants.EventFlagActionListWaits;
                                    }

                                    if (e.Actions[index].Id == SmartConstants.ActionRepeatTimedActionList)
                                    {
                                        actualAction = smartFactory.ActionFactory(SmartConstants.ActionCreateTimed, null, null);
                                        actualAction.GetParameter(0).Value = nextTriggerId;
                                        actualAction.GetParameter(1).Value = 500;
                                        actualAction.GetParameter(2).Value = 500;
                                        actualAction.Comment = SmartConstants.CommentInlineRepeatActionList;
                                        
                                        var eventTimedTriggerRepeat = smartFactory.EventFactory(null, SmartConstants.EventTriggerTimed);
                                        eventTimedTriggerRepeat.Parent = script;
                                        eventTimedTriggerRepeat.GetParameter(0).Value = nextTriggerId++;
                                        additionalEvents.Add(eventTimedTriggerRepeat);
                                        
                                        SmartAction callTimedActionListAgain = smartFactory.ActionFactory(SmartConstants.ActionCallTimedActionList, smartFactory.SourceFactory(SmartConstants.SourceSelf), smartFactory.TargetFactory(SmartConstants.TargetSelf));
                                        callTimedActionListAgain.GetParameter(0).Value = firstInlineActionListId.Value;
                                        if (firstInlineActionListUpdateType.HasValue)
                                            callTimedActionList.GetParameter(1).Value = firstInlineActionListUpdateType.Value;
                                        if (firstInlineActionListAllowOverride.HasValue)
                                            callTimedActionList.GetParameter(2).Value = firstInlineActionListAllowOverride.Value ? 1 : 0;

                                        callTimedActionListAgain.Parent = eventTimedTriggerRepeat;
                                        eventTimedTriggerRepeat.Actions.Add(callTimedActionListAgain);
                                    }
                                    
                                    AdjustCoreCompatibleAction(actualAction);
                        
                                    after.Parent = new SmartScript(new SmartScriptSolutionItem(currentInlineActionListId.Value, SmartScriptType.TimedActionList), smartFactory, smartDataManager, messageBoxService, editorFeatures, importer);
                                    after.AddAction(actualAction);
                        
                                    var serialized = after.ToSmartScriptLines(null, currentInlineActionListId.Value, SmartScriptType.TimedActionList, timedEventId++, false, 0);

                                    if (serialized.Length != 1)
                                        throw new InvalidOperationException();

                                    lines.Add(serialized[0]);
                                    afterTimeMin = 0;
                                    afterTimeMax = 0;
                                    
                                    if (e.Actions[index].Id == SmartConstants.ActionRepeatTimedActionList)
                                        FlushLines(null);
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

        public async Task<IQuery> GenerateSql(ISmartScriptSolutionItem item, SmartScript script)
        {
            return await new ExporterHelper(script, databaseProvider, item, this, currentCoreVersion, nameRegistry, conditionQueryGenerator).GetSql();
        }

        public int GetDatabaseScriptTypeId(SmartScriptType type)
        {
            return (int)type;
        }

        public SmartScriptType GetScriptTypeFromId(int id)
        {
            return (SmartScriptType)id;
        }
    }
}