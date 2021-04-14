using System;
using System.Collections.Generic;
using System.Linq;
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

        public TrinityCoreSmartScriptExporter(ISmartFactory smartFactory, ISmartDataManager smartDataManager)
        {
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
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

            foreach (SmartEvent e in script.Events)
            {
                if (e.Actions.Count == 0)
                    continue;

                e.ActualId = eventId;

                for (var index = 0; index < e.Actions.Count; ++index)
                {
                    SmartEvent actualEvent = e;

                    if (previousWasWait)
                    {
                        actualEvent = smartFactory.EventFactory(SmartConstants.EventTriggerTimed);
                        actualEvent.GetParameter(0).Value = nextTriggerId++;
                    }
                    else if (index > 0)
                        actualEvent = smartFactory.EventFactory(SmartConstants.EventLink);

                    int linkTo = e.Actions.Count - 1 == index ? 0 : eventId + 1;

                    SmartAction actualAction = e.Actions[index].Copy();

                    if (actualAction.Id == SmartConstants.ActionWait)
                    {
                        linkTo = 0;
                        SmartAction waitAction = actualAction;
                        actualAction = smartFactory.ActionFactory(SmartConstants.ActionTriggerTimed,
                            smartFactory.SourceFactory(SmartConstants.SourceNone),
                            smartFactory.TargetFactory(SmartConstants.TargetNone));
                        actualAction.GetParameter(0).Value = nextTriggerId;
                        actualAction.GetParameter(1).Value = waitAction.GetParameter(0).Value;
                        actualAction.GetParameter(2).Value = waitAction.GetParameter(0).Value;
                        actualAction.Comment = SmartConstants.CommentWait;
                        previousWasWait = true;
                    }
                    else
                    {
                        if (actualAction.Id == SmartConstants.ActionComment)
                        {
                            SmartAction commentAction = actualAction;
                            actualAction = smartFactory.ActionFactory(SmartConstants.ActionNone,
                                smartFactory.SourceFactory(SmartConstants.SourceNone),
                                smartFactory.TargetFactory(SmartConstants.TargetNone));
                            actualAction.Comment = commentAction.Comment;
                        }
                        previousWasWait = false;
                    }

                    var actionData = smartDataManager.GetRawData(SmartType.SmartAction, actualAction.Id);
                    
                    if (actionData.TargetIsSource)
                    {
                        smartFactory.UpdateTarget(actualAction.Target, actualAction.Source.Id);
                        for (int i = 0; i < actualAction.Target.ParametersCount; ++i)
                            actualAction.Target.GetParameter(i).Copy(actualAction.Source.GetParameter(i));
                        
                        smartFactory.UpdateSource(actualAction.Source, 0);
                    }
                    
                    if (actionData.ImplicitSource != null)
                        smartFactory.UpdateSource(actualAction.Source, smartDataManager.GetDataByName(SmartType.SmartSource, actionData.ImplicitSource).Id);
                    else if (actionData.SourceStoreInAction)
                    {
                        actualAction.GetParameter(2).Value = actualAction.Source.Id;
                        actualAction.GetParameter(3).Value = actualAction.Source.GetParameter(0).Value;
                        actualAction.GetParameter(4).Value = actualAction.Source.GetParameter(1).Value;
                        actualAction.GetParameter(5).Value = actualAction.Source.GetParameter(2).Value;
                    }

                    SmartEvent eventToSerialize = actualEvent.ShallowCopy();
                    eventToSerialize.Parent = script;
                    eventToSerialize.Actions.Add(actualAction);

                    var serialized = eventToSerialize.ToSmartScriptLines(script.EntryOrGuid, script.SourceType, eventId, linkTo);
                    var serializedConditions = actualEvent.ToConditionLines(SmartConstants.ConditionSourceSmartScript, script.EntryOrGuid, script.SourceType, eventId);

                    if (serialized.Length != 1)
                        throw new InvalidOperationException();

                    lines.Add(serialized[0]);
                    if (serializedConditions != null)
                        conditions.AddRange(serializedConditions);

                    eventId++;
                }
            }

            return (lines.ToArray(), conditions.ToArray());
        }

        public string GenerateSql(SmartScript script)
        {
            return new ExporterHelper(script, this).GetSql();
        }
    }
}