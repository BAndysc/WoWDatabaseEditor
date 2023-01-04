using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models.Helpers;

namespace WDE.SmartScriptEditor.Models
{
    public abstract class SmartScriptBase
    {
        protected readonly ISmartFactory smartFactory;
        private readonly IEditorFeatures editorFeatures;
        protected readonly ISmartDataManager smartDataManager;
        protected readonly IMessageBoxService messageBoxService;
        private readonly ISmartScriptImporter importer;

        public readonly ObservableCollection<SmartEvent> Events;
        
        private readonly SmartSelectionHelper selectionHelper;
        
        public abstract SmartScriptType SourceType { get; }
        public event Action? ScriptSelectedChanged;
        public event Action<SmartEvent?, SmartAction?, EventChangedMask>? EventChanged;
        public event Action? InvalidateVisual;
        
        public ObservableCollection<object> AllSmartObjectsFlat { get; } 
        
        public ObservableCollection<SmartAction> AllActions { get; }

        public ObservableCollection<GlobalVariable> GlobalVariables { get; } = new();

        ~SmartScriptBase()
        {
            selectionHelper.Dispose();
        }

        public SmartGenericJsonData GetEventData(SmartEvent e) =>
            smartDataManager.GetRawData(SmartType.SmartEvent, e.Id);
        
        public SmartGenericJsonData? TryGetEventData(SmartEvent e) =>
            smartDataManager.Contains(SmartType.SmartEvent, e.Id) ? smartDataManager.GetRawData(SmartType.SmartEvent, e.Id) : null;
        
        public SmartGenericJsonData? TryGetSourceData(SmartSource s) =>
            smartDataManager.Contains(SmartType.SmartSource, s.Id) ? smartDataManager.GetRawData(SmartType.SmartSource, s.Id) : null;
        
        public SmartGenericJsonData? TryGetTargetData(SmartTarget t) =>
            smartDataManager.Contains(SmartType.SmartTarget, t.Id) ? smartDataManager.GetRawData(SmartType.SmartTarget, t.Id) : null;
        
        public SmartScriptBase(ISmartFactory smartFactory,
            IEditorFeatures editorFeatures,
            ISmartDataManager smartDataManager,
            IMessageBoxService messageBoxService,
            ISmartScriptImporter importer)
        {
            this.smartFactory = smartFactory;
            this.editorFeatures = editorFeatures;
            this.smartDataManager = smartDataManager;
            this.messageBoxService = messageBoxService;
            this.importer = importer;
            Events = new ObservableCollection<SmartEvent>();
            selectionHelper = new SmartSelectionHelper(this);
            selectionHelper.ScriptSelectedChanged += CallScriptSelectedChanged;
            selectionHelper.EventChanged += (e, a, mask) =>
            {
                RenumerateEvents();
                EventChanged?.Invoke(e, a, mask);
            };
            AllSmartObjectsFlat = selectionHelper.AllSmartObjectsFlat;
            AllActions = selectionHelper.AllActions;
            
            Events.ToStream(false)
                .Subscribe((e) =>
                {
                    if (e.Type == CollectionEventType.Add)
                    {
                        e.Item.Parent = this;
                        if (e.Item.IsEvent)
                        {
                            if (TryGetEventGroup(e.Index, out var group, out _))
                                e.Item.Group = group;
                            else
                                e.Item.Group = null;
                        }
                        else if (e.Item.IsGroup)
                        {
                            RegroupAllEvents();
                        }
                    }
                    else if (e.Type == CollectionEventType.Remove)
                    {
                        e.Item.Parent = null;
                        e.Item.Group = null; 
                        if (e.Item.IsGroup)
                        {
                            RegroupAllEvents();
                        }
                    }
                });

            GlobalVariables.ToStream(true).Subscribe(e =>
            {
                GlobalVariableChanged(null, null);
                if (e.Type == CollectionEventType.Add)
                    e.Item.PropertyChanged += GlobalVariableChanged;
                else
                    e.Item.PropertyChanged -= GlobalVariableChanged;
                InvalidateVisual?.Invoke();
            });
        }

        private void RegroupAllEvents()
        {
            SmartGroup? lastGroup = null;
            foreach (var e in Events)
            {
                if (e.IsEvent)
                    e.Group = lastGroup;
                else if (e.IsBeginGroup)
                {
                    lastGroup = new SmartGroup(e);
                }
                else if (e.IsEndGroup)
                {
                    e.Group = lastGroup;
                    lastGroup = null;
                }
            }
        }

        private void GlobalVariableChanged(object? sender, PropertyChangedEventArgs? _)
        {
            foreach (var e in Events)
                e.InvalidateAllParameters();
            InvalidateVisual?.Invoke();
        }

        private class NestedEventIds
        {
            private List<int> stack = new List<int>();

            public NestedEventIds(int initial)
            {
                stack.Add(initial);
            }

            public int Next() => stack[^1]++;

            public void AddNestLevel() => stack.Add(1);

            public void Pop() => stack.RemoveAt(stack.Count - 1);

            public void PopAll()
            {
                while (stack.Count > 1)
                    stack.RemoveAt(stack.Count - 1);
            }
        }

        public virtual int GetFirstPossibleTimedActionListId() => 0;

        private void RenumerateEvents()
        {
            int virtualLineId = 1;
            int specialConditionId = 1;
            NestedEventIds eventId = new NestedEventIds(GlobalVariables.Count + 1);

            HashSet<int> usedTimedActionLists = CollectUsedTimedActionListIds().ToHashSet();

            int firstUnusedActionList = GetFirstPossibleTimedActionListId() -1;
            int GetNextUnusedTimedActionList()
            {
                do
                {
                    firstUnusedActionList++;
                } while (usedTimedActionLists.Contains(firstUnusedActionList));

                usedTimedActionLists.Add(firstUnusedActionList);
                return firstUnusedActionList;
            }

            foreach (var e in Events)
            {
                if (e.IsGroup)
                {
                    eventId.Next();
                    continue;
                }
                
                e.VirtualLineId = virtualLineId;
                if (e.Actions.Count == 0)
                {
                    e.DestinationEventId = null;
                    virtualLineId++;
                }
                else
                {
                    int indent = 0;
                    bool inInlineActionList = false;
                    int? inlineActionListId = null;
                    int? firstId = null;
                    foreach (var a in e.Actions)
                    {
                        a.IsInInlineActionList = inInlineActionList;
                        a.DestinationTimedActionListId = inlineActionListId;
                        
                        if (a.ActionFlags.HasFlagFast(ActionFlags.DecreaseIndent) && indent > 0)
                            indent--;
                        
                        a.Indent = indent;
                        
                        if (a.ActionFlags.HasFlagFast(ActionFlags.IncreaseIndent))
                            indent++;
                        
                        a.VirtualLineId = virtualLineId++;
                        if (a.Id == SmartConstants.ActionLink ||
                            a.Id == SmartConstants.ActionComment ||
                            (inInlineActionList && a.Id == SmartConstants.ActionAfter))
                        {
                            a.DestinationEventId = null;
                            a.DestinationTimedActionListId = null;
                        }
                        else if (a.Id == SmartConstants.ActionBeginInlineActionList)
                        {
                            eventId.PopAll();
                            a.DestinationEventId = eventId.Next();
                            firstId ??= a.DestinationEventId;
                            inlineActionListId = GetNextUnusedTimedActionList();
                            a.DestinationTimedActionListId = inlineActionListId;
                            eventId.AddNestLevel();
                            inInlineActionList = true;
                        }
                        else if (inInlineActionList && a.Id == SmartConstants.ActionAfterMovement)
                        {
                            eventId.Pop();
                            a.DestinationEventId = eventId.Next();
                            a.IsInInlineActionList = false;
                            eventId.AddNestLevel();
                        }
                        else if (inInlineActionList && a.Id == SmartConstants.ActionRepeatTimedActionList)
                        {
                            eventId.Pop();
                            a.DestinationEventId = eventId.Next();
                            a.IsInInlineActionList = false;
                        }
                        else
                        {
                            a.DestinationEventId = eventId.Next();
                            firstId ??= a.DestinationEventId;
                        }

                        if (a.Source.Conditions != null && a.Source.Conditions.Count > 0)
                            a.Source.DestinationConditionId = specialConditionId++;
                        if (a.Target.Conditions != null && a.Target.Conditions.Count > 0)
                            a.Target.DestinationConditionId = specialConditionId++;
                    }
                    e.DestinationEventId = firstId;
                    eventId.PopAll();
                }
            }
        }

        private void CallScriptSelectedChanged()
        {
            ScriptSelectedChanged?.Invoke();
            InvalidateVisual?.Invoke();
        }

        public bool TryGetEventGroup(SmartEvent e, out SmartGroup group, out SmartEvent endGroup)
        {
            group = null!;
            endGroup = null!;
            var indexOfEvent = Events.IndexOf(e);
            if (indexOfEvent == -1)
                return false;

            return TryGetEventGroup(indexOfEvent, out group, out endGroup);
        }
        
        public bool TryGetEventGroup(int indexOfEvent, out SmartGroup group, out SmartEvent endGroup)
        {
            group = null!;
            endGroup = null!;
            if (indexOfEvent >= Events.Count)
                return false;
            
            while (indexOfEvent >= 0)
            {
                if (Events[indexOfEvent].IsEndGroup)
                    return false;
                if (Events[indexOfEvent].IsBeginGroup)
                {
                    group = new SmartGroup(Events[indexOfEvent]);
                    while (++indexOfEvent < Events.Count)
                    {
                        if (Events[indexOfEvent].IsEndGroup)
                        {
                            endGroup = Events[indexOfEvent];
                            return true;
                        }
                        else if (Events[indexOfEvent].IsBeginGroup)
                        {
                            return false;
                        }
                    }
                    return false;
                }
                indexOfEvent--;
            }

            return false;
        }

        public bool TryGetEvent(int index, out SmartEvent e)
        {
            if (index >= 0 && index < Events.Count)
            {
                e = Events[index];
                return true;
            }

            e = null!;
            return false;
        }

        public SmartEvent? FindMatchingBackwards(int startSearch, Func<SmartEvent, bool> predicate)
        {
            for (int i = startSearch; i >= 0; --i)
                if (predicate(Events[i]))
                    return Events[i];
            return null;
        }

        public SmartGroup InsertGroupBegin(ref int index, string header, string? description, bool expanded)
        {   
            var previousGroup = FindMatchingBackwards(index - 1, e => e.IsGroup);
            if (previousGroup != null && previousGroup.IsBeginGroup)
            {
                // close unclosed group
                if (InsertGroupEnd(index))
                    index++;
            }
            
            var groupBegin = SmartEvent.NewBeginGroup();
            var groupBeginView = new SmartGroup(groupBegin);
            groupBeginView.Header = header;
            groupBeginView.Description = description;
            groupBeginView.IsExpanded = expanded;
            Events.Insert(index, groupBegin);
            return groupBeginView;
        }

        public bool InsertGroupEnd(int index)
        {
            var previousGroup = FindMatchingBackwards(index - 1, e => e.IsGroup);
            if (previousGroup == null || previousGroup.IsEndGroup)
                return false;
            
            var groupEnd = SmartEvent.NewEndGroup();
            Events.Insert(index, groupEnd);
            return true;
        }

        public List<SmartEvent> InsertFromClipboard(int index, IEnumerable<ISmartScriptLine> lines, IEnumerable<IConditionLine>? conditions, IEnumerable<IConditionLine>? targetConditionLines)
        {
            List<SmartEvent> newEvents = new();
            SmartEvent? currentEvent = null;
            var prevIndex = 0;
            var conds = importer.ImportConditions(this, conditions?.ToList() ?? new List<IConditionLine>());

            Dictionary<int, List<ICondition>>? targetConditions = null;
            if (targetConditionLines != null)
                targetConditions = targetConditionLines.GroupBy(x => x.SourceGroup).ToDictionary(x => x.Key, x => x.ToList<ICondition>());

            bool hasOpenedGroup = false;
            foreach (ISmartScriptLine line in lines)
            {
                if (line.EventType == SmartConstants.EventGroupBegin &&
                    line.Comment.TryParseBeginGroupComment(out var header, out var description))
                {
                    if (TryGetEventGroup(index, out var group, out var endGroup))
                    {
                        if (InsertGroupEnd(index))
                            index++;
                        Events.Remove(endGroup);
                    }
                    group = InsertGroupBegin(ref index, header, description, true);
                    index++;
                    newEvents.Add(group.InnerEvent);
                    hasOpenedGroup = true;
                    continue;
                }
                
                if (line.EventType == SmartConstants.EventGroupEnd &&
                    line.Comment.TryParseEndGroupComment())
                {
                    if (InsertGroupEnd(index))
                        index++;
                    hasOpenedGroup = false;
                    continue;
                }
                
                if (currentEvent == null || prevIndex != line.Id)
                {
                    prevIndex = line.Id;
                    currentEvent = SafeEventFactory(line);
                    if (currentEvent == null)
                        continue;
                    
                    currentEvent.Parent = this;
                    Events.Insert(index++, currentEvent);
                    newEvents.Add(currentEvent);
                    if (conds.TryGetValue(prevIndex, out var conditionList))
                        foreach (var cond in conditionList)
                            currentEvent.Conditions.Add(cond);   
                }

                if (line.ActionType != -1)
                {
                    SmartAction? action = SafeActionFactory(line);
                    if (action != null)
                    {
                        action.Comment = line.Comment.Contains(" // ")
                            ? line.Comment.Substring(line.Comment.IndexOf(" // ") + 4).Trim()
                            : "";
                        
                        if (targetConditions != null &&
                            line.SourceConditionId > 0 &&
                            targetConditions.TryGetValue(line.SourceConditionId, out var srcConditions))
                            action.Source.Conditions = srcConditions;
                        
                        if (targetConditions != null &&
                            line.TargetConditionId > 0 &&
                            targetConditions.TryGetValue(line.TargetConditionId, out var targtConditions))
                            action.Target.Conditions = targtConditions;
                        
                        currentEvent.AddAction(action);
                    }
                }
            }

            if (hasOpenedGroup)
            {
                InsertGroupEnd(index++);
            }
            return newEvents;
        }

        public void SafeUpdateSource(SmartSource source, int targetId)
        {
            try
            {
                smartFactory.UpdateSource(source, targetId);
            }
            catch (Exception e)
            {
                LOG.LogWarning(e);
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Error)
                    .SetTitle("Unknown source")
                    .SetMainInstruction($"Source {targetId} unknown, this may lead to invalid script opened!!")
                    .Build());
            }
        }
        
        public SmartAction? SafeActionFactory(int actionId, SmartSource source, SmartTarget target)
        {
            try
            {
                return smartFactory.ActionFactory(actionId, source, target);
            }
            catch (Exception e)
            {
                LOG.LogWarning(e);
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown action")
                    .SetMainInstruction($"Action {actionId} unknown, skipping action")
                    .Build());
            }

            return null;
        }
        
        public SmartAction? SafeActionFactory(ISmartScriptLine line)
        {
            try
            {
                return smartFactory.ActionFactory(line);
            }
            catch (Exception e)
            {
                LOG.LogWarning(e);
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown action")
                    .SetMainInstruction($"Skipping action {line.ActionType} (or source {line.SourceType} or target {line.TargetType}): " + e.Message)
                    .Build());
            }

            return null;
        }
        
        public SmartCondition? SafeConditionFactory(ICondition line)
        {
            try
            {
                return smartFactory.ConditionFactory(line);
            }
            catch (Exception e)
            {
                LOG.LogWarning(e);
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown condition")
                    .SetMainInstruction($"Condition {line.ConditionType} unknown, skipping condition")
                    .Build());
            }

            return null;
        }
        
        public SmartCondition? SafeConditionFactory(int id)
        {
            try
            {
                return smartFactory.ConditionFactory(id);
            }
            catch (Exception e)
            {
                LOG.LogWarning(e);
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown condition")
                    .SetMainInstruction($"Condition {id} unknown, skipping condition")
                    .Build());
            }

            return null;
        }
        
        public SmartEvent? SafeEventFactory(ISmartScriptLine line)
        {
            try
            {
                return smartFactory.EventFactory(line);
            }
            catch (Exception e)
            {
                LOG.LogWarning(e);
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown event")
                    .SetMainInstruction($"Event {line.EventType} unknown, skipping event")
                    .Build());
            }

            return null;
        }

        public IEnumerable<int> CollectUsedTimedActionListIds()
        {
            return Events
                .SelectMany(e => e.Actions)
                .Where(a => a.Id == SmartConstants.ActionCallTimedActionList ||
                            a.Id == SmartConstants.ActionCallRandomTimedActionList ||
                            a.Id == SmartConstants.ActionCallRandomRangeTimedActionList)
                .SelectMany(a =>
                {
                    if (a.Id == SmartConstants.ActionCallRandomTimedActionList)
                        return Enumerable.Range(0, a.ParametersCount).Select(i => (int)a.GetParameter(i).Value);
                    if (a.Id == SmartConstants.ActionCallRandomRangeTimedActionList &&
                        a.GetParameter(1).Value - a.GetParameter(0).Value < 20)
                        return Enumerable.Range((int)a.GetParameter(0).Value,
                            (int)(a.GetParameter(1).Value - a.GetParameter(0).Value + 1));
                    return new int[] { (int)a.GetParameter(0).Value };
                })
                .Where(id => id != 0);
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
            private readonly SmartScriptBase smartScript;

            public BulkEditing(SmartScriptBase smartScript, string name)
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
