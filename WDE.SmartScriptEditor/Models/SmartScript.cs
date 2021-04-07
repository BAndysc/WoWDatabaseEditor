using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Conditions.Data;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models.Helpers;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScript
    {
        private readonly ISmartFactory smartFactory;
        private readonly ISmartDataManager smartDataManager;
        private readonly IMessageBoxService messageBoxService;

        public readonly int EntryOrGuid;
        public readonly SmartScriptType SourceType;
        public readonly ObservableCollection<SmartEvent> Events;
        
        private readonly SmartSelectionHelper selectionHelper;

        public event Action ScriptSelectedChanged;
        
        public ObservableCollection<object> AllSmartObjectsFlat { get; } 
        
        ~SmartScript()
        {
            selectionHelper.Dispose();
        }

        public SmartGenericJsonData GetEventData(SmartEvent e) =>
            smartDataManager.GetRawData(SmartType.SmartEvent, e.Id);
        
        public SmartScript(ISmartScriptSolutionItem item, 
            ISmartFactory smartFactory,
            ISmartDataManager smartDataManager,
            IMessageBoxService messageBoxService)
        {
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
            this.messageBoxService = messageBoxService;
            EntryOrGuid = (int)item.Entry;
            SourceType = item.SmartType;
            Events = new ObservableCollection<SmartEvent>();
            selectionHelper = new SmartSelectionHelper(this);
            selectionHelper.ScriptSelectedChanged += CallScriptSelectedChanged;
            AllSmartObjectsFlat = selectionHelper.AllSmartObjectsFlat;
            
            Events.ToStream()
                .Subscribe((e) =>
                {
                    if (e.Type == CollectionEventType.Add)
                        e.Item.Parent = this;
                });
        }

        private void CallScriptSelectedChanged()
        {
            ScriptSelectedChanged?.Invoke();
        }

        public void Load(IList<ISmartScriptLine> lines, IList<IConditionLine> conditions)
        {
            int? entry = null;
            SmartScriptType? source = null;

            var conds = ParseConditions(conditions);
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
            
            Dictionary<int, int> linkToTriggerTimedEventId = null;
            if (doubleLinks.Count > 0)
            {
                int nextFreeTriggerTimedEvent = lines.Where(e => e.Id == SmartConstants.EventTriggerTimed)
                    .Select(e => (int)e.EventParam1)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

                linkToTriggerTimedEventId = doubleLinks.Select(linkId => (linkId, nextFreeTriggerTimedEvent++))
                    .ToDictionary(pair => pair.linkId, pair => pair.Item2);
            }
            
            foreach (ISmartScriptLine line in lines)
            {
                SmartEvent currentEvent = null;
                
                if (!entry.HasValue)
                    entry = line.EntryOrGuid;
                else
                    Debug.Assert(entry.Value == line.EntryOrGuid);

                if (!source.HasValue)
                    source = (SmartScriptType) line.ScriptSourceType;
                else
                    Debug.Assert((int) source.Value == line.ScriptSourceType);

                if (!linkToSmartEventParent.TryGetValue(line.Id, out currentEvent))
                {
                    currentEvent = SafeEventFactory(line);
                        
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
                        
                        Events.Add(currentEvent);
                    }
                    else
                        continue;
                }
                
                string comment = line.Comment.Contains(" // ") ? line.Comment.Substring(line.Comment.IndexOf(" // ") + 4).Trim() : "";

                if (!string.IsNullOrEmpty(comment) && line.ActionType == SmartConstants.ActionNone)
                {
                    line.ActionType = SmartConstants.ActionComment;
                }

                SmartAction action = SafeActionFactory(line);
                
                if (action != null)
                {
                    var raw = smartDataManager.GetRawData(SmartType.SmartAction, line.ActionType);
                    if (raw.TargetIsSource)
                    {
                        smartFactory.UpdateSource(action.Source, action.Target.Id);
                        for (int i = 0; i < action.Source.ParametersCount; ++i)
                            action.Source.GetParameter(i).Copy(action.Target.GetParameter(i));
                        smartFactory.UpdateTarget(action.Target, 0);
                    }
                    
                    if (comment != SmartConstants.CommentWait)
                        action.Comment = comment;
                    if (action.Id == SmartConstants.ActionTriggerTimed && comment == SmartConstants.CommentWait)
                        triggerIdToActionParent[action.GetParameter(0).Value] = currentEvent;
                    currentEvent.AddAction(action);
                }
                
                if (line.Link != 0 && !doubleLinks.Contains(line.Link))
                {
                    linkToSmartEventParent[line.Link] = currentEvent;
                }
                else if (line.Link != 0)
                {
                    var actionCallLinkedAsTrigger = smartFactory.ActionFactory(SmartConstants.ActionTriggerTimed,
                        smartFactory.SourceFactory(SmartConstants.ActionNone),
                        smartFactory.TargetFactory(SmartConstants.TargetNone));
                    actionCallLinkedAsTrigger.GetParameter(0).Value = linkToTriggerTimedEventId![line.Link];
                    currentEvent.AddAction(actionCallLinkedAsTrigger);
                }
            }

            var sortedTriggers = triggerIdToEvent.Keys.ToList();
            sortedTriggers.Reverse();
            foreach (int triggerId in sortedTriggers)
            {
                SmartEvent @event = triggerIdToEvent[triggerId];
                if (!triggerIdToActionParent.ContainsKey(triggerId))
                    continue;

                SmartEvent caller = triggerIdToActionParent[triggerId];

                SmartAction lastAction = caller.Actions[caller.Actions.Count - 1];

                if (lastAction.Id != SmartConstants.ActionTriggerTimed ||
                    lastAction.GetParameter(1).Value != lastAction.GetParameter(2).Value)
                    continue;

                long waitTime = lastAction.GetParameter(1).Value;
                SmartAction waitAction = smartFactory.ActionFactory(SmartConstants.ActionWait,
                    smartFactory.SourceFactory(SmartConstants.SourceNone),
                    smartFactory.TargetFactory(SmartConstants.TargetNone));
                waitAction.GetParameter(0).Value = waitTime;

                caller.Actions.RemoveAt(caller.Actions.Count - 1);
                caller.AddAction(waitAction);
                Events.Remove(@event);
                foreach (SmartAction a in @event.Actions)
                    caller.AddAction(a);
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

        public List<SmartEvent> InsertFromClipboard(int index, IEnumerable<ISmartScriptLine> lines, IEnumerable<IConditionLine> conditions)
        {
            List<SmartEvent> newEvents = new();
            SmartEvent currentEvent = null;
            var prevIndex = 0;
            var conds = ParseConditions(conditions);

            foreach (ISmartScriptLine line in lines)
            {
                if (currentEvent == null || prevIndex != line.Id)
                {
                    prevIndex = line.Id;
                    currentEvent = SafeEventFactory(line);
                    if (currentEvent == null)
                        continue;
                    
                    Events.Insert(index++, currentEvent);
                    newEvents.Add(currentEvent);
                    if (conds.TryGetValue(prevIndex, out var conditionList))
                        foreach (var cond in conditionList)
                            currentEvent.Conditions.Add(cond);   
                }

                if (line.ActionType != -1)
                {
                    SmartAction action = SafeActionFactory(line);
                    if (action != null)
                    {
                        action.Comment = line.Comment.Contains(" // ")
                            ? line.Comment.Substring(line.Comment.IndexOf(" // ") + 4).Trim()
                            : "";
                        currentEvent.AddAction(action);
                    }
                }
            }
            return newEvents;
        }

        private Dictionary<int, List<SmartCondition>> ParseConditions(IEnumerable<IConditionLine> conditions)
        {
            Dictionary<int, List<SmartCondition>> conds = new();
            if (conditions != null)
            {
                int prevElseGroup = 0;
                foreach (IConditionLine line in conditions)
                {
                    SmartCondition condition = SafeConditionFactory(line);

                    if (condition == null)
                        continue;

                    if (!conds.ContainsKey(line.SourceGroup - 1))
                        conds[line.SourceGroup - 1] = new List<SmartCondition>();

                    if (prevElseGroup != line.ElseGroup && conds[line.SourceGroup - 1].Count > 0)
                    {
                        conds[line.SourceGroup - 1].Add(SafeConditionFactory(-1));
                        prevElseGroup = line.ElseGroup;
                    }

                    conds[line.SourceGroup - 1].Add(condition);
                }
            }

            return conds;
        }

        public SmartAction SafeActionFactory(ISmartScriptLine line)
        {
            try
            {
                return smartFactory.ActionFactory(line);
            }
            catch (Exception)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown action")
                    .SetMainInstruction($"Action {line.ActionType} unknown, skipping action")
                    .Build());
            }

            return null;
        }
        
        public SmartCondition SafeConditionFactory(IConditionLine line)
        {
            try
            {
                return smartFactory.ConditionFactory(line);
            }
            catch (Exception)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown condition")
                    .SetMainInstruction($"Condition {line.ConditionType} unknown, skipping condition")
                    .Build());
            }

            return null;
        }
        
        public SmartCondition SafeConditionFactory(int id)
        {
            try
            {
                return smartFactory.ConditionFactory(id);
            }
            catch (Exception)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown condition")
                    .SetMainInstruction($"Condition {id} unknown, skipping condition")
                    .Build());
            }

            return null;
        }
        
        public SmartEvent SafeEventFactory(ISmartScriptLine line)
        {
            try
            {
                return smartFactory.EventFactory(line);
            }
            catch (Exception)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown event")
                    .SetMainInstruction($"Event {line.EventType} unknown, skipping event")
                    .Build());
            }

            return null;
        }

        public event Action BulkEditingStarted = delegate { };
        public event Action<string> BulkEditingFinished = delegate { };

        public IDisposable BulkEdit(string name)
        {
            return new BulkEditing(this, name);
        }

        private class BulkEditing : IDisposable
        {
            private readonly string name;
            private readonly SmartScript smartScript;

            public BulkEditing(SmartScript smartScript, string name)
            {
                this.smartScript = smartScript;
                this.name = name;
                this.smartScript.BulkEditingStarted.Invoke();
            }

            public void Dispose()
            {
                smartScript.BulkEditingFinished.Invoke(name);
            }
        }
    }
}