using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.History
{
    public class EventAiHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly EventAiBase script;
        private readonly IEventAiFactory eventAiFactory;

        public EventAiHistoryHandler(EventAiBase script, IEventAiFactory eventAiFactory)
        {
            this.script = script;
            this.eventAiFactory = eventAiFactory;
            this.script.Events.CollectionChanged += Events_CollectionChanged;
            this.script.BulkEditingStarted += OnBulkEditingStarted;
            this.script.BulkEditingFinished += OnBulkEditingFinished;

            foreach (EventAiEvent ev in this.script.Events)
                BindEvent(ev);
        }

        public void Dispose()
        {
            script.Events.CollectionChanged -= Events_CollectionChanged;
            script.BulkEditingStarted -= OnBulkEditingStarted;
            script.BulkEditingFinished -= OnBulkEditingFinished;
        }

        private void Events_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (EventAiEvent ev in e.NewItems!)
                    AddedEvent(ev, e.NewStartingIndex);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (EventAiEvent ev in e.OldItems!)
                    RemovedEvent(ev, e.OldStartingIndex);
            }
        }

        private void UnbindEvent(EventAiEvent eventAiEvent)
        {
            foreach (EventAiAction act in eventAiEvent.Actions)
                UnbindAction(act);

            eventAiEvent.Actions.CollectionChanged -= Actions_CollectionChanged;

            eventAiEvent.BulkEditingStarted -= OnBulkEditingStarted;
            eventAiEvent.BulkEditingFinished -= OnBulkEditingFinished;
            eventAiEvent.Chance.OnValueChanged -= Parameter_OnValueChanged;
            eventAiEvent.Flags.OnValueChanged -= Parameter_OnValueChanged;
            eventAiEvent.Phases.OnValueChanged -= Parameter_OnValueChanged;
            eventAiEvent.OnIdChanged -= EventOnOnIdChanged;

            for (var i = 0; i < EventAiEvent.EventParamsCount; ++i)
                eventAiEvent.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;
        }

        private void AddedAction(EventAiAction eventAiAction, EventAiEvent parent, int index)
        {
            PushAction(new ActionAddedAction(parent, eventAiAction, index));
            BindAction(eventAiAction);
        }

        private void RemovedAction(EventAiAction eventAiAction, EventAiEvent parent, int index)
        {
            UnbindAction(eventAiAction);
            PushAction(new ActionRemovedAction(parent, eventAiAction, index));
        }

        private void BindAction(EventAiAction eventAiAction)
        {
            eventAiAction.BulkEditingStarted += OnBulkEditingStarted;
            eventAiAction.BulkEditingFinished += OnBulkEditingFinished;

            for (var i = 0; i < eventAiAction.ParametersCount; ++i)
                eventAiAction.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            eventAiAction.CommentParameter.OnValueChanged += ParameterString_OnValueChanged;
            eventAiAction.OnIdChanged += ActionOnOnIdChanged;
        }

        private void UnbindAction(EventAiAction eventAiAction)
        {
            eventAiAction.OnIdChanged -= ActionOnOnIdChanged;
            eventAiAction.BulkEditingStarted -= OnBulkEditingStarted;
            eventAiAction.BulkEditingFinished -= OnBulkEditingFinished;

            for (var i = 0; i < eventAiAction.ParametersCount; ++i)
                eventAiAction.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;

            eventAiAction.CommentParameter.OnValueChanged -= ParameterString_OnValueChanged;
        }
        
        private void AddedEvent(EventAiEvent eventAiEvent, int index)
        {
            PushAction(new EventAddedAction(script, eventAiEvent, index));
            BindEvent(eventAiEvent);
        }

        private void RemovedEvent(EventAiEvent eventAiEvent, int index)
        {
            UnbindEvent(eventAiEvent);
            PushAction(new EventRemovedAction(script, eventAiEvent, index));
        }

        private void BindEvent(EventAiEvent eventAiEvent)
        {
            eventAiEvent.OnIdChanged += EventOnOnIdChanged;
            eventAiEvent.BulkEditingStarted += OnBulkEditingStarted;
            eventAiEvent.BulkEditingFinished += OnBulkEditingFinished;
            eventAiEvent.Chance.OnValueChanged += Parameter_OnValueChanged;
            eventAiEvent.Flags.OnValueChanged += Parameter_OnValueChanged;
            eventAiEvent.Phases.OnValueChanged += Parameter_OnValueChanged;

            for (var i = 0; i < EventAiEvent.EventParamsCount; ++i)
                eventAiEvent.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            eventAiEvent.Actions.CollectionChanged += Actions_CollectionChanged;

            foreach (EventAiAction action in eventAiEvent.Actions)
                BindAction(action);
        }

        private void EventOnOnIdChanged(EventAiBaseElement element, uint oldId, uint newId)
        {
            PushAction(new EventActionIdChangedAction<EventAiEvent>((EventAiEvent)element, oldId, newId, (e, id) => eventAiFactory.UpdateEvent(e, id)));
        }

        private void ActionOnOnIdChanged(EventAiBaseElement action, uint old, uint @new)
        {
            PushAction(new EventActionIdChangedAction<EventAiAction>((EventAiAction)action, old, @new, (a, id) => eventAiFactory.UpdateAction(a, id)));
        }
        
        private void OnBulkEditingFinished(string editName)
        {
            EndBulkEdit(editName.RemoveTags());
        }

        private void OnBulkEditingStarted()
        {
            StartBulkEdit();
        }

        private void Actions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (EventAiAction ev in e.NewItems!)
                    if (ev.Parent != null)
                        AddedAction(ev, ev.Parent, e.NewStartingIndex);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (EventAiAction ac in e.OldItems!)
                    if (ac.Parent != null)
                        RemovedAction(ac, ac.Parent, e.OldStartingIndex);
            }
        }

        private void Parameter_OnValueChanged(ParameterValueHolder<long> sender, long old, long @new)
        {
            PushAction(new ParameterChangedAction(sender, old, @new));
        }

        private void ParameterFloat_OnValueChange(ParameterValueHolder<float> sender, float old, float @new)
        {
            PushAction(new GenericParameterChangedAction<float>(sender, old, @new));
        }

        private void ParameterString_OnValueChanged(ParameterValueHolder<string> sender, string old, string @new)
        {
            PushAction(new GenericParameterChangedAction<string>(sender, old, @new));
        }
        
        private class EventAddedAction : IHistoryAction
        {
            private readonly int index;
            private readonly string readable;
            private readonly EventAiBase script;
            private readonly EventAiEvent eventAiEvent;

            public EventAddedAction(EventAiBase script, EventAiEvent eventAiEvent, int index)
            {
                this.script = script;
                this.eventAiEvent = eventAiEvent;
                this.index = index;
                readable = eventAiEvent.Readable;
            }

            public string GetDescription()
            {
                return "Added event " + readable.RemoveTags();
            }

            public void Redo()
            {
                eventAiEvent.IsSelected = false;
                script.Events.Insert(index, eventAiEvent);
            }

            public void Undo()
            {
                script.Events.Remove(eventAiEvent);
            }
        }

        private class EventRemovedAction : IHistoryAction
        {
            private readonly int index;
            private readonly EventAiBase script;
            private readonly EventAiEvent eventAiEvent;

            public EventRemovedAction(EventAiBase script, EventAiEvent eventAiEvent, int index)
            {
                this.script = script;
                this.eventAiEvent = eventAiEvent;
                this.index = index;
            }

            public string GetDescription()
            {
                return "Removed event " + eventAiEvent.Readable.RemoveTags();
            }

            public void Redo()
            {
                script.Events.Remove(eventAiEvent);
            }

            public void Undo()
            {
                eventAiEvent.IsSelected = false;
                script.Events.Insert(index, eventAiEvent);
            }
        }

        private class GenericParameterChangedAction<T> : IHistoryAction where T : notnull
        {
            private readonly T @new;
            private readonly T old;
            private readonly ParameterValueHolder<T> param;

            public GenericParameterChangedAction(ParameterValueHolder<T> param, T old, T @new)
            {
                this.param = param;
                this.old = old;
                this.@new = @new;
            }

            public string GetDescription()
            {
                return "Changed " + param.Name + " from " + param.Parameter.ToString(old) + " to " + param.Parameter.ToString(@new);
            }

            public void Redo()
            {
                param.Value = @new;
            }

            public void Undo()
            {
                param.Value = old;
            }
        }

        private class ParameterChangedAction : GenericParameterChangedAction<long>
        {
            public ParameterChangedAction(ParameterValueHolder<long> param, long old, long @new) : base(param, old, @new)
            {
            }
        }
    }
    
    public class ActionAddedAction : IHistoryAction
    {
        private readonly int index;
        private readonly EventAiEvent parent;
        private readonly EventAiAction eventAiAction;
        private readonly string readable;

        public ActionAddedAction(EventAiEvent parent, EventAiAction eventAiAction, int index)
        {
            this.parent = parent;
            this.eventAiAction = eventAiAction;
            this.index = index;
            this.readable = eventAiAction.Readable.RemoveTags();
        }

        public string GetDescription()
        {
            return "Added action " + readable;
        }

        public void Redo()
        {
            eventAiAction.IsSelected = false;
            parent.Actions.Insert(index, eventAiAction);
        }

        public void Undo()
        {
            parent.Actions.Remove(eventAiAction);
        }
    }
    
    public class ActionRemovedAction : IHistoryAction
    {
        private readonly int index;
        private readonly EventAiEvent parent;
        private readonly EventAiAction eventAiAction;

        public ActionRemovedAction(EventAiEvent parent, EventAiAction eventAiAction, int index)
        {
            this.parent = parent;
            this.eventAiAction = eventAiAction;
            this.index = index;
        }

        public string GetDescription()
        {
            return "Removed action " + eventAiAction.Readable.RemoveTags();
        }

        public void Redo()
        {
            parent.Actions.Remove(eventAiAction);
        }

        public void Undo()
        {
            eventAiAction.IsSelected = false;
            parent.Actions.Insert(index, eventAiAction);
        }
    }
    
    public class EventActionIdChangedAction<T> : IHistoryAction where T : EventAiBaseElement
    {
        private readonly T element;
        private readonly uint old;
        private readonly uint @new;
        private readonly Action<T, uint> changer;

        public EventActionIdChangedAction(T element, uint old, uint @new, Action<T, uint> changer)
        {
            this.element = element;
            this.old = old;
            this.@new = @new;
            this.changer = changer;
        }
        
        public void Undo()
        {
            changer(element, old);
        }

        public void Redo()
        {
            changer(element, @new);
        }

        public string GetDescription() => "Changed type";
    }
}