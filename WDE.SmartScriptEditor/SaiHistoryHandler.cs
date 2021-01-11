using System;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor
{
    public class SaiHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly SmartScript script;

        public SaiHistoryHandler(SmartScript script)
        {
            this.script = script;
            this.script.Events.CollectionChanged += Events_CollectionChanged;
            this.script.BulkEditingStarted += OnBulkEditingStarted;
            this.script.BulkEditingFinished += OnBulkEditingFinished;

            foreach (var ev in this.script.Events)
                BindEvent(ev);
        }

        public void Dispose()
        {
            script.Events.CollectionChanged -= Events_CollectionChanged;
            script.BulkEditingStarted -= OnBulkEditingStarted;
            script.BulkEditingFinished -= OnBulkEditingFinished;
        }

        private void Events_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                foreach (var ev in e.NewItems)
                    AddedEvent(ev as SmartEvent, e.NewStartingIndex);
            else if (e.Action == NotifyCollectionChangedAction.Remove)
                foreach (var ev in e.OldItems)
                    RemovedEvent(ev as SmartEvent, e.OldStartingIndex);
        }

        private void UnbindEvent(SmartEvent smartEvent)
        {
            foreach (var act in smartEvent.Actions)
                UnbindAction(act);

            smartEvent.Actions.CollectionChanged -= Actions_CollectionChanged;

            smartEvent.BulkEditingStarted -= OnBulkEditingStarted;
            smartEvent.BulkEditingFinished -= OnBulkEditingFinished;
            smartEvent.Chance.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.Flags.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.Phases.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.CooldownMin.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.CooldownMax.OnValueChanged -= Parameter_OnValueChanged;

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
        }

        private void UnbindAction(SmartAction smartAction)
        {
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

            foreach (var smartAction in smartEvent.Actions)
                BindAction(smartAction);
        }

        private void OnBulkEditingFinished(string editName) { EndBulkEdit(editName); }

        private void OnBulkEditingStarted() { StartBulkEdit(); }

        private void Actions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                foreach (var ev in e.NewItems)
                    AddedAction(ev as SmartAction, (ev as SmartAction).Parent, e.NewStartingIndex);
            else if (e.Action == NotifyCollectionChangedAction.Remove)
                foreach (var ac in e.OldItems)
                    RemovedAction(ac as SmartAction, (ac as SmartAction).Parent, e.OldStartingIndex);
        }


        private void Parameter_OnValueChanged(object sender, ParameterChangedValue<int> e)
        {
            PushAction(new ParameterChangedAction(sender as Parameter, e.Old, e.New));
        }

        private void ParameterFloat_OnValueChange(object sender, ParameterChangedValue<float> e)
        {
            PushAction(new GenericParameterChangedAction<float>(sender as FloatParameter, e.Old, e.New));
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
                // @Todo: how to localize this?
                return "Added event " + readable;
            }

            public void Redo() { script.Events.Insert(index, smartEvent); }

            public void Undo() { script.Events.Remove(smartEvent); }
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
                // @Todo: how to localize this?
                return "Removed event " + smartEvent.Readable;
            }

            public void Redo() { script.Events.Remove(smartEvent); }

            public void Undo() { script.Events.Insert(index, smartEvent); }
        }

        private class GenericParameterChangedAction<T> : IHistoryAction
        {
            private readonly T @new;
            private readonly T old;
            private readonly GenericBaseParameter<T> param;

            public GenericParameterChangedAction(GenericBaseParameter<T> param, T old, T @new)
            {
                this.param = param;
                this.old = old;
                this.@new = @new;
            }

            public string GetDescription()
            {
                // @Todo: how to localize this?
                return "Changed " + param.Name + " from " + old + " to " + @new;
            }

            public void Redo() { param.SetValue(@new); }

            public void Undo() { param.SetValue(old); }
        }

        private class ParameterChangedAction : GenericParameterChangedAction<int>
        {
            public ParameterChangedAction(GenericBaseParameter<int> param, int old, int @new) : base(param, old, @new) { }
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
            // @Todo: how to localize this?
            return "Added action " + smartAction.Readable;
        }

        public void Redo() { parent.Actions.Insert(index, smartAction); }

        public void Undo() { parent.Actions.Remove(smartAction); }
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

        public string GetDescription() { return "Removed action " + smartAction.Readable; }

        public void Redo() { parent.Actions.Remove(smartAction); }

        public void Undo() { parent.Actions.Insert(index, smartAction); }
    }
}