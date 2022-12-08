using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
                    lastGroup = new SmartGroup(e);
                else if (e.IsEndGroup)
                    lastGroup = null;
            }
        }

        private void GlobalVariableChanged(object? sender, PropertyChangedEventArgs? _)
        {
            foreach (var e in Events)
                e.InvalidateReadable();
            InvalidateVisual?.Invoke();
        }

        private void RenumerateEvents()
        {
            int index = 1;
            foreach (var e in Events)
            {
                e.LineId = index;
                if (e.Actions.Count == 0)
                {
                    index++;
                }
                else
                {
                    int indent = 0;
                    foreach (var a in e.Actions)
                    {
                        if (a.ActionFlags.HasFlagFast(ActionFlags.DecreaseIndent) && indent > 0)
                            indent--;
                        
                        a.Indent = indent;
                        
                        if (a.ActionFlags.HasFlagFast(ActionFlags.IncreaseIndent))
                            indent++;
                        
                        a.LineId = index;
                        index++;
                    }   
                }
            }
        }

        private void CallScriptSelectedChanged()
        {
            ScriptSelectedChanged?.Invoke();
            InvalidateVisual?.Invoke();
        }
        
        public void Clear()
        {
            GlobalVariables.RemoveAll();
            Events.RemoveAll();
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
                            Console.WriteLine("[ERROR IN DATA] Nested groups are not allowed!");
                            return false;
                        }
                    }
                    Console.WriteLine("[ERROR IN DATA] Begin group without an end!");
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

        public SmartGroup InsertGroupBegin(int index, string header, string? description)
        {   
            var previousGroup = FindMatchingBackwards(index - 1, e => e.IsGroup);
            if (previousGroup != null && previousGroup.IsBeginGroup)
            {
                // close unclosed group
                InsertGroupEnd(index++);
            }
            
            var groupBegin = SmartEvent.NewBeginGroup();
            var groupBeginView = new SmartGroup(groupBegin);
            groupBeginView.Header = header;
            groupBeginView.Description = description;
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

            foreach (ISmartScriptLine line in lines)
            {
                if (line.EventType == SmartConstants.EventGroupBegin &&
                    line.Comment.TryParseBeginGroupComment(out var header, out var description))
                {
                    if (TryGetEventGroup(index, out var group, out var endGroup))
                    {
                        InsertGroupEnd(index++);
                        Events.Remove(endGroup);
                    }
                    InsertGroupBegin(index++, header, description);
                    continue;
                }
                
                if (line.EventType == SmartConstants.EventGroupEnd &&
                    line.Comment.TryParseEndGroupComment())
                {
                    InsertGroupEnd(index++);
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
                Console.WriteLine(e);
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
                Console.WriteLine(e);
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
                Console.WriteLine(e);
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
                Console.WriteLine(e);
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
                Console.WriteLine(e);
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
                Console.WriteLine(e);
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
