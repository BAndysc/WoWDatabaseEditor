using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
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
        private readonly IConditionQueryGenerator conditionQueryGenerator;

        public TrinityCoreSmartScriptExporter(ISmartFactory smartFactory,
            ISmartDataManager smartDataManager,
            ICurrentCoreVersion currentCoreVersion,
            IConditionQueryGenerator conditionQueryGenerator)
        {
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
            this.currentCoreVersion = currentCoreVersion;
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
                foreach (SmartEvent e in script.Events)
                {
                    if (e.Actions.Count == 0)
                        continue;

                    SmartEvent originalEvent = e.ShallowCopy();
                    originalEvent.Parent = script;
                    List<SmartEvent> delayedWaits = new();
                    SmartEvent lastEvent = originalEvent;
                    
                    long accumulatedWaits = 0;
                    for (var index = 0; index < e.Actions.Count; ++index)
                    {
                        if (previousWasWait)
                        {
                            var eventTimed = smartFactory.EventFactory(SmartConstants.EventTriggerTimed);
                            eventTimed.Parent = script;
                            eventTimed.GetParameter(0).Value = nextTriggerId++;
                            delayedWaits.Add(eventTimed);
                            lastEvent = eventTimed;
                        }
                     
                        if (e.Actions[index].Id == SmartConstants.ActionWait)
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
                    
                    var serializedConditions = e.ToConditionLines(SmartConstants.ConditionSourceSmartScript, script.EntryOrGuid, script.SourceType, eventId);
                    if (serializedConditions != null)
                        conditions.AddRange(serializedConditions);
                    
                    foreach (var toSerialize in new SmartEvent[] {originalEvent}.Concat(delayedWaits))
                    {
                        var serialized = toSerialize.ToSmartScriptLines(script.EntryOrGuid, script.SourceType, eventId, true);
                        eventId += serialized.Length;
                        lines.AddRange(serialized);
                    }
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

        public string GenerateSql(SmartScript script)
        {
            return new ExporterHelper(script, this, currentCoreVersion, conditionQueryGenerator).GetSql();
        }
    }
}