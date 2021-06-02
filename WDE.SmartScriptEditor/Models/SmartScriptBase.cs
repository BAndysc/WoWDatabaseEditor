using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Conditions.Data;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models.Helpers;

namespace WDE.SmartScriptEditor.Models
{
    public abstract class SmartScriptBase
    {
        protected readonly ISmartFactory smartFactory;
        protected readonly ISmartDataManager smartDataManager;
        protected readonly IMessageBoxService messageBoxService;

        public readonly ObservableCollection<SmartEvent> Events;
        
        private readonly SmartSelectionHelper selectionHelper;
        
        public abstract SmartScriptType SourceType { get; }
        public event Action? ScriptSelectedChanged;
        public event Action<SmartEvent?, SmartAction?, EventChangedMask>? EventChanged;
        
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
            ISmartDataManager smartDataManager,
            IMessageBoxService messageBoxService)
        {
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
            this.messageBoxService = messageBoxService;
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
            
            Events.ToStream()
                .Subscribe((e) =>
                {
                    if (e.Type == CollectionEventType.Add)
                        e.Item.Parent = this;
                });

            GlobalVariables.ToStream().Subscribe(e =>
            {
                GlobalVariableChanged(null, null);
                if (e.Type == CollectionEventType.Add)
                    e.Item.PropertyChanged += GlobalVariableChanged;
                else
                    e.Item.PropertyChanged -= GlobalVariableChanged;
            });
        }

        private void GlobalVariableChanged(object? sender, PropertyChangedEventArgs? _)
        {
            foreach (var e in Events)
                e.InvalidateReadable();
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
                    foreach (var a in e.Actions)
                    {
                        a.LineId = index;
                        index++;
                    }   
                }
            }
        }

        private void CallScriptSelectedChanged()
        {
            ScriptSelectedChanged?.Invoke();
        }
        
        public List<SmartEvent> InsertFromClipboard(int index, IEnumerable<ISmartScriptLine> lines, IEnumerable<IConditionLine>? conditions)
        {
            List<SmartEvent> newEvents = new();
            SmartEvent? currentEvent = null;
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
                        currentEvent.AddAction(action);
                    }
                }
            }
            return newEvents;
        }

        public Dictionary<int, List<SmartCondition>> ParseConditions(IEnumerable<IConditionLine>? conditions)
        {
            Dictionary<int, List<SmartCondition>> conds = new();
            if (conditions != null)
            {
                int prevElseGroup = 0;
                foreach (IConditionLine line in conditions)
                {
                    SmartCondition? condition = SafeConditionFactory(line);

                    if (condition == null)
                        continue;

                    if (!conds.ContainsKey(line.SourceGroup - 1))
                        conds[line.SourceGroup - 1] = new List<SmartCondition>();

                    if (prevElseGroup != line.ElseGroup && conds[line.SourceGroup - 1].Count > 0)
                    {
                        var or = SafeConditionFactory(-1);
                        if (or != null)
                            conds[line.SourceGroup - 1].Add(or);
                        prevElseGroup = line.ElseGroup;
                    }

                    conds[line.SourceGroup - 1].Add(condition);
                }
            }

            return conds;
        }

        public void SafeUpdateSource(SmartSource source, int targetId)
        {
            try
            {
                smartFactory.UpdateSource(source, targetId);
            }
            catch (Exception)
            {
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
            catch (Exception)
            {
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
        
        public SmartCondition? SafeConditionFactory(IConditionLine line)
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
        
        public SmartCondition? SafeConditionFactory(int id)
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
        
        public SmartEvent? SafeEventFactory(ISmartScriptLine line)
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
