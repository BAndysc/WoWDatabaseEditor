using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.History;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor
{
    public class SaiHistoryHandler : HistoryHandler
    {
        private readonly SmartScript _script;

        public SaiHistoryHandler(SmartScript script)
        {
            _script = script;
            _script.Events.CollectionChanged += Events_CollectionChanged;

            foreach (SmartEvent ev in _script.Events)
                BindEvent(ev);
        }

        private void Events_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (object ev in e.NewItems)
                    AddedEvent(ev as SmartEvent, e.NewStartingIndex);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (object ev in e.OldItems)
                    RemoveEvent(ev as SmartEvent);
            }
        }

        private void RemoveEvent(SmartEvent smartEvent)
        {
            smartEvent.Actions.CollectionChanged -= Actions_CollectionChanged;

            smartEvent.Chance.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.Flags.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.Phases.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.CooldownMin.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.CooldownMax.OnValueChanged -= Parameter_OnValueChanged;

            for (int i = 0; i < SmartEvent.SmartEventParamsCount; ++i)
                smartEvent.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;
        }

        private void AddedAction(SmartAction smartAction, SmartEvent parent, int index)
        {
            PushAction(new ActionAddedAction(parent, smartAction, index));

            BindAction(smartAction);
        }

        private void BindAction(SmartAction smartAction)
        {
            for (int i = 0; i < smartAction.ParametersCount; ++i)
                smartAction.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            for (int i = 0; i < smartAction.Source.ParametersCount; ++i)
                smartAction.Source.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            for (int i = 0; i < smartAction.Target.ParametersCount; ++i)
                smartAction.Target.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            for (int i = 0; i < 4; ++i)
                smartAction.Target.Position[i].OnValueChanged += ParameterFloat_OnValueChange;
        }

        private void RemoveAction(SmartAction smartAction)
        {
            for (int i = 0; i < smartAction.ParametersCount; ++i)
                smartAction.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;

            for (int i = 0; i < smartAction.Source.ParametersCount; ++i)
                smartAction.Source.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;

            for (int i = 0; i < smartAction.Target.ParametersCount; ++i)
                smartAction.Target.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;

            for (int i = 0; i < 4; ++i)
                smartAction.Target.Position[i].OnValueChanged -= ParameterFloat_OnValueChange;
        }

        private void AddedEvent(SmartEvent smartEvent, int index)
        {
            PushAction(new EventAddedAction(_script, smartEvent, index));
            BindEvent(smartEvent);
        }

        private void BindEvent(SmartEvent smartEvent)
        {
            smartEvent.Chance.OnValueChanged += Parameter_OnValueChanged;
            smartEvent.Flags.OnValueChanged += Parameter_OnValueChanged;
            smartEvent.Phases.OnValueChanged += Parameter_OnValueChanged;
            smartEvent.CooldownMin.OnValueChanged += Parameter_OnValueChanged;
            smartEvent.CooldownMax.OnValueChanged += Parameter_OnValueChanged;

            for (int i = 0; i < SmartEvent.SmartEventParamsCount; ++i)
                smartEvent.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;

            smartEvent.Actions.CollectionChanged += Actions_CollectionChanged;

            foreach (var smartAction in smartEvent.Actions)
                BindAction(smartAction);
        }

        private void Actions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (object ev in e.NewItems)
                    AddedAction(ev as SmartAction, (ev as SmartAction).Parent, e.NewStartingIndex);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (object ac in e.OldItems)
                    RemoveAction(ac as SmartAction);
            }
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
            private readonly SmartScript _script;
            private readonly SmartEvent _smartEvent;
            private readonly int _index;

            public EventAddedAction(SmartScript script, SmartEvent smartEvent, int index)
            {
                _script = script;
                _smartEvent = smartEvent;
                _index = index;
            }

            public string GetDescription()
            {
                // @Todo: how to localize this?
                return "Added event " + _smartEvent.Readable;
            }

            public void Redo()
            {
                _script.Events.Insert(_index, _smartEvent);
            }

            public void Undo()
            {
                _script.Events.Remove(_smartEvent);
            }
        }

        private class GenericParameterChangedAction<T> : IHistoryAction
        {
            private readonly GenericBaseParameter<T> _param;
            private readonly T _old;
            private readonly T _new;

            public GenericParameterChangedAction(GenericBaseParameter<T> param, T old, T @new)
            {
                _param = param;
                _old = old;
                _new = @new;
            }

            public string GetDescription()
            {
                // @Todo: how to localize this?
                return "Changed " + _param.Name + " from " + _old + " to " + _new;
            }

            public void Redo()
            {
                _param.SetValue(_new);
            }

            public void Undo()
            {
                _param.SetValue(_old);
            }
        }

        private class ParameterChangedAction : GenericParameterChangedAction<int>
        {
            public ParameterChangedAction(GenericBaseParameter<int> param, int old, int @new) : base(param, old, @new)
            {
            }
        }
    }

    public  class ActionAddedAction : IHistoryAction
    {
        private readonly SmartEvent _parent;
        private readonly SmartAction _smartAction;
        private readonly int _index;

        public ActionAddedAction(SmartEvent parent, SmartAction smartAction, int index)
        {
            _parent = parent;
            _smartAction = smartAction;
            _index = index;
        }

        public string GetDescription()
        {
            // @Todo: how to localize this?
            return "Added action " + _smartAction.Readable;
        }

        public void Redo()
        {
            _parent.Actions.Insert(_index, _smartAction);
        }

        public void Undo()
        {
            _parent.Actions.Remove(_smartAction);
        }
    }
}
