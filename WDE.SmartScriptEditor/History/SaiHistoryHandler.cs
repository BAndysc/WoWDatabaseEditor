using System;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.History
{
    public class SaiHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly SmartScript script;
        private readonly ISmartFactory smartFactory;

        public SaiHistoryHandler(SmartScript script, ISmartFactory smartFactory)
        {
            this.script = script;
            this.smartFactory = smartFactory;
            this.script.Events.CollectionChanged += Events_CollectionChanged;
            this.script.BulkEditingStarted += OnBulkEditingStarted;
            this.script.BulkEditingFinished += OnBulkEditingFinished;

            foreach (SmartEvent ev in this.script.Events)
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
                foreach (SmartEvent ev in e.NewItems!)
                    AddedEvent(ev, e.NewStartingIndex);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SmartEvent ev in e.OldItems!)
                    RemovedEvent(ev, e.OldStartingIndex);
            }
        }

        private void UnbindEvent(SmartEvent smartEvent)
        {
            foreach (SmartAction act in smartEvent.Actions)
                UnbindAction(act);

            foreach (SmartCondition c in smartEvent.Conditions)
                UnbindCondition(c);
                
            smartEvent.Actions.CollectionChanged -= Actions_CollectionChanged;
            smartEvent.Conditions.CollectionChanged -= Conditions_CollectionChanged;

            smartEvent.BulkEditingStarted -= OnBulkEditingStarted;
            smartEvent.BulkEditingFinished -= OnBulkEditingFinished;
            smartEvent.Chance.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.Flags.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.Phases.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.CooldownMin.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.CooldownMax.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.OnIdChanged -= SmartEventOnOnIdChanged;

            for (var i = 0; i < SmartEvent.SmartEventParamsCount; ++i)
                smartEvent.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;
        }

        private void AddedAction(SmartAction smartAction, SmartEvent parent, int index)
        {
            PushAction(new ActionAddedAction(parent, smartAction, index));
            BindAction(smartAction);
        }

        private void RemovedAction(SmartAction smartAction, SmartEvent parent, int index)
        {
            UnbindAction(smartAction);
            PushAction(new ActionRemovedAction(parent, smartAction, index));
        }

        private void BindAction(SmartAction smartAction)
        {
            smartAction.BulkEditingStarted += OnBulkEditingStarted;
            smartAction.BulkEditingFinished += OnBulkEditingFinished;

            for (var i = 0; i < smartAction.ParametersCount; ++i)
                smartAction.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            for (var i = 0; i < smartAction.Source.ParametersCount; ++i)
                smartAction.Source.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            for (var i = 0; i < smartAction.Target.ParametersCount; ++i)
                smartAction.Target.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            for (var i = 0; i < 4; ++i)
                smartAction.Target.Position[i].OnValueChanged += ParameterFloat_OnValueChange;

            smartAction.CommentParameter.OnValueChanged += ParameterString_OnValueChanged;
            smartAction.OnIdChanged += SmartActionOnOnIdChanged;
            smartAction.Source.OnIdChanged += SmartSourceOnOnIdChanged;
            smartAction.Target.OnIdChanged += SmartTargetOnOnIdChanged;
        }
        
        private void UnbindAction(SmartAction smartAction)
        {
            smartAction.Target.OnIdChanged -= SmartTargetOnOnIdChanged;
            smartAction.Source.OnIdChanged -= SmartSourceOnOnIdChanged;
            smartAction.OnIdChanged -= SmartActionOnOnIdChanged;
            smartAction.BulkEditingStarted -= OnBulkEditingStarted;
            smartAction.BulkEditingFinished -= OnBulkEditingFinished;

            for (var i = 0; i < smartAction.ParametersCount; ++i)
                smartAction.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;

            for (var i = 0; i < smartAction.Source.ParametersCount; ++i)
                smartAction.Source.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;

            for (var i = 0; i < smartAction.Target.ParametersCount; ++i)
                smartAction.Target.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;

            for (var i = 0; i < 4; ++i)
                smartAction.Target.Position[i].OnValueChanged -= ParameterFloat_OnValueChange;
            
            smartAction.CommentParameter.OnValueChanged -= ParameterString_OnValueChanged;
        }
        
        private void AddedCondition(SmartCondition smartCondition, SmartEvent parent, int index)
        {
            PushAction(new ConditionAddedAction(parent, smartCondition, index));
            BindCondition(smartCondition);
        }

        private void RemovedCondition(SmartCondition smartCondition, SmartEvent parent, int index)
        {
            UnbindCondition(smartCondition);
            PushAction(new ConditionRemovedAction(parent, smartCondition, index));
        }
        
        private void BindCondition(SmartCondition smartCondition)
        {
            smartCondition.OnIdChanged += SmartConditionOnOnIdChanged;
            smartCondition.BulkEditingStarted += OnBulkEditingStarted;
            smartCondition.BulkEditingFinished += OnBulkEditingFinished;

            for (var i = 0; i < smartCondition.ParametersCount; ++i)
                smartCondition.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            smartCondition.Inverted.OnValueChanged += Parameter_OnValueChanged;
            smartCondition.ConditionTarget.OnValueChanged += Parameter_OnValueChanged;
        }

        private void UnbindCondition(SmartCondition smartCondition)
        {
            smartCondition.BulkEditingStarted -= OnBulkEditingStarted;
            smartCondition.BulkEditingFinished -= OnBulkEditingFinished;

            for (var i = 0; i < smartCondition.ParametersCount; ++i)
                smartCondition.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;

            smartCondition.Inverted.OnValueChanged -= Parameter_OnValueChanged;
            smartCondition.ConditionTarget.OnValueChanged -= Parameter_OnValueChanged;
            smartCondition.OnIdChanged -= SmartConditionOnOnIdChanged;
        }
        
        private void AddedEvent(SmartEvent smartEvent, int index)
        {
            PushAction(new EventAddedAction(script, smartEvent, index));
            BindEvent(smartEvent);
        }

        private void RemovedEvent(SmartEvent smartEvent, int index)
        {
            UnbindEvent(smartEvent);
            PushAction(new EventRemovedAction(script, smartEvent, index));
        }

        private void BindEvent(SmartEvent smartEvent)
        {
            smartEvent.OnIdChanged += SmartEventOnOnIdChanged;
            smartEvent.BulkEditingStarted += OnBulkEditingStarted;
            smartEvent.BulkEditingFinished += OnBulkEditingFinished;
            smartEvent.Chance.OnValueChanged += Parameter_OnValueChanged;
            smartEvent.Flags.OnValueChanged += Parameter_OnValueChanged;
            smartEvent.Phases.OnValueChanged += Parameter_OnValueChanged;
            smartEvent.CooldownMin.OnValueChanged += Parameter_OnValueChanged;
            smartEvent.CooldownMax.OnValueChanged += Parameter_OnValueChanged;

            for (var i = 0; i < SmartEvent.SmartEventParamsCount; ++i)
                smartEvent.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            smartEvent.Actions.CollectionChanged += Actions_CollectionChanged;
            smartEvent.Conditions.CollectionChanged += Conditions_CollectionChanged;

            foreach (SmartAction smartAction in smartEvent.Actions)
                BindAction(smartAction);
                
            foreach (SmartCondition smartCondition in smartEvent.Conditions)
                BindCondition(smartCondition);
        }

        private void SmartEventOnOnIdChanged(SmartBaseElement element, int oldId, int newId)
        {
            PushAction(new SmartIdChangedAction<SmartEvent>((SmartEvent)element, oldId, newId, (e, id) => smartFactory.UpdateEvent(e, id)));
        }

        private void SmartActionOnOnIdChanged(SmartBaseElement action, int old, int @new)
        {
            PushAction(new SmartIdChangedAction<SmartAction>((SmartAction)action, old, @new, (a, id) => smartFactory.UpdateAction(a, id)));
        }
        
        private void SmartConditionOnOnIdChanged(SmartBaseElement condition, int old, int @new)
        {
            PushAction(new SmartIdChangedAction<SmartCondition>((SmartCondition)condition, old, @new, (a, id) => smartFactory.UpdateCondition(a, id)));
        }   
        
        private void SmartTargetOnOnIdChanged(SmartBaseElement target, int old, int @new)
        {
            PushAction(new SmartIdChangedAction<SmartTarget>((SmartTarget)target, old, @new, (a, id) => smartFactory.UpdateTarget(a, id)));
        }
        
        private void SmartSourceOnOnIdChanged(SmartBaseElement source, int old, int @new)
        {
            PushAction(new SmartIdChangedAction<SmartSource>((SmartSource)source, old, @new, (a, id) => smartFactory.UpdateSource(a, id)));
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
                foreach (SmartAction ev in e.NewItems!)
                    if (ev.Parent != null)
                        AddedAction(ev, ev.Parent, e.NewStartingIndex);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SmartAction ac in e.OldItems!)
                    if (ac.Parent != null)
                        RemovedAction(ac, ac.Parent, e.OldStartingIndex);
            }
        }

        private void Conditions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (SmartCondition ev in e.NewItems!)
                    if (ev.Parent != null)
                        AddedCondition(ev, ev.Parent, e.NewStartingIndex);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SmartCondition ac in e.OldItems!)
                    if (ac.Parent != null)
                        RemovedCondition(ac, ac.Parent, e.OldStartingIndex);
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
            private readonly SmartScript script;
            private readonly SmartEvent smartEvent;

            public EventAddedAction(SmartScript script, SmartEvent smartEvent, int index)
            {
                this.script = script;
                this.smartEvent = smartEvent;
                this.index = index;
                readable = smartEvent.Readable;
            }

            public string GetDescription()
            {
                return "Added event " + readable.RemoveTags();
            }

            public void Redo()
            {
                smartEvent.IsSelected = false;
                script.Events.Insert(index, smartEvent);
            }

            public void Undo()
            {
                script.Events.Remove(smartEvent);
            }
        }

        private class EventRemovedAction : IHistoryAction
        {
            private readonly int index;
            private readonly SmartScript script;
            private readonly SmartEvent smartEvent;

            public EventRemovedAction(SmartScript script, SmartEvent smartEvent, int index)
            {
                this.script = script;
                this.smartEvent = smartEvent;
                this.index = index;
            }

            public string GetDescription()
            {
                return "Removed event " + smartEvent.Readable.RemoveTags();
            }

            public void Redo()
            {
                script.Events.Remove(smartEvent);
            }

            public void Undo()
            {
                smartEvent.IsSelected = false;
                script.Events.Insert(index, smartEvent);
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
        private readonly SmartEvent parent;
        private readonly SmartAction smartAction;

        public ActionAddedAction(SmartEvent parent, SmartAction smartAction, int index)
        {
            this.parent = parent;
            this.smartAction = smartAction;
            this.index = index;
        }

        public string GetDescription()
        {
            return "Added action " + smartAction.Readable.RemoveTags();
        }

        public void Redo()
        {
            smartAction.IsSelected = false;
            parent.Actions.Insert(index, smartAction);
        }

        public void Undo()
        {
            parent.Actions.Remove(smartAction);
        }
    }


    public class ActionRemovedAction : IHistoryAction
    {
        private readonly int index;
        private readonly SmartEvent parent;
        private readonly SmartAction smartAction;

        public ActionRemovedAction(SmartEvent parent, SmartAction smartAction, int index)
        {
            this.parent = parent;
            this.smartAction = smartAction;
            this.index = index;
        }

        public string GetDescription()
        {
            return "Removed action " + smartAction.Readable.RemoveTags();
        }

        public void Redo()
        {
            parent.Actions.Remove(smartAction);
        }

        public void Undo()
        {
            smartAction.IsSelected = false;
            parent.Actions.Insert(index, smartAction);
        }
    }
    
    public class ConditionRemovedAction : IHistoryAction
    {
        private readonly int index;
        private readonly SmartEvent parent;
        private readonly SmartCondition smartCondition;

        public ConditionRemovedAction(SmartEvent parent, SmartCondition smartCondition, int index)
        {
            this.parent = parent;
            this.smartCondition = smartCondition;
            this.index = index;
        }

        public string GetDescription()
        {
            return "Removed condition " + smartCondition.Readable.RemoveTags();
        }

        public void Redo()
        {
            parent.Conditions.Remove(smartCondition);
        }

        public void Undo()
        {
            smartCondition.IsSelected = false;
            parent.Conditions.Insert(index, smartCondition);
        }
    }

    public class SmartIdChangedAction<T> : IHistoryAction where T : SmartBaseElement
    {
        private readonly T element;
        private readonly int old;
        private readonly int @new;
        private readonly Action<T, int> changer;

        public SmartIdChangedAction(T element, int old, int @new, Action<T, int> changer)
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
    
    public class ConditionAddedAction : IHistoryAction
    {
        private readonly int index;
        private readonly SmartEvent parent;
        private readonly SmartCondition smartCondition;

        public ConditionAddedAction(SmartEvent parent, SmartCondition smartCondition, int index)
        {
            this.parent = parent;
            this.smartCondition = smartCondition;
            this.index = index;
        }

        public string GetDescription()
        {
            return "Added condition " + smartCondition.Readable.RemoveTags();
        }

        public void Redo()
        {
            smartCondition.IsSelected = false;
            parent.Conditions.Insert(index, smartCondition);
        }

        public void Undo()
        {
            parent.Conditions.Remove(smartCondition);
        }
    }
}