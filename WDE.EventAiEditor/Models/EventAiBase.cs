using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.MVVM.Observable;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Models.Helpers;

namespace WDE.EventAiEditor.Models
{
    public abstract class EventAiBase
    {
        protected readonly IEventAiFactory EventAiFactory;
        protected readonly IEventAiDataManager EventAiDataManager;
        protected readonly IMessageBoxService messageBoxService;

        public readonly ObservableCollection<EventAiEvent> Events;
        
        private readonly EventAiSelectionHelper selectionHelper;
        
        public event Action? ScriptSelectedChanged;
        public event Action<EventAiEvent?, EventAiAction?, EventChangedMask>? EventChanged;
        
        public ObservableCollection<object> AllObjectsFlat { get; } 
        
        public ObservableCollection<EventAiAction> AllActions { get; }

        ~EventAiBase()
        {
            selectionHelper.Dispose();
        }

        public EventActionGenericJsonData GetEventData(EventAiEvent e) =>
            EventAiDataManager.GetRawData(EventOrAction.Event, e.Id);
        
        public EventActionGenericJsonData? TryGetEventData(EventAiEvent e) =>
            EventAiDataManager.Contains(EventOrAction.Event, e.Id) ? EventAiDataManager.GetRawData(EventOrAction.Event, e.Id) : null;
        
        public EventAiBase(IEventAiFactory eventAiFactory,
            IEventAiDataManager eventAiDataManager,
            IMessageBoxService messageBoxService)
        {
            this.EventAiFactory = eventAiFactory;
            this.EventAiDataManager = eventAiDataManager;
            this.messageBoxService = messageBoxService;
            Events = new ObservableCollection<EventAiEvent>();
            selectionHelper = new EventAiSelectionHelper(this);
            selectionHelper.ScriptSelectedChanged += CallScriptSelectedChanged;
            selectionHelper.EventChanged += (e, a, mask) =>
            {
                RenumerateEvents();
                EventChanged?.Invoke(e, a, mask);
            };
            AllObjectsFlat = selectionHelper.AllObjectsFlat;
            AllActions = selectionHelper.AllActions;
            
            Events.ToStream(false)
                .Subscribe((e) =>
                {
                    if (e.Type == CollectionEventType.Add)
                        e.Item.Parent = this;
                });
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
        }
        
        public EventAiAction? SafeActionFactory(uint actionId)
        {
            try
            {
                return EventAiFactory.ActionFactory(actionId);
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
        
        public EventAiAction? SafeActionFactory(IEventAiLine line, int index)
        {
            try
            {
                return EventAiFactory.ActionFactory(line, index);
            }
            catch (Exception e)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Warning)
                    .SetTitle("Unknown action")
                    .SetMainInstruction("Skipping action: " + e.Message)
                    .Build());
            }

            return null;
        }
        
        public EventAiEvent? SafeEventFactory(IEventAiLine line)
        {
            try
            {
                return EventAiFactory.EventFactory(line);
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
            private readonly EventAiBase EventAi;

            public BulkEditing(EventAiBase EventAi, string name)
            {
                this.EventAi = EventAi;
                this.name = name;
                this.EventAi.BulkEditingStarted.Invoke();
            }

            public void Dispose()
            {
                EventAi.BulkEditingFinished.Invoke(name);
            }
        }
    }
}
