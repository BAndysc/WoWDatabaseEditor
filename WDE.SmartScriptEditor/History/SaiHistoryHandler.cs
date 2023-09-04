using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.History
{
    public class SaiHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly SmartScriptBase script;
        private readonly ISmartFactory smartFactory;
        private System.IDisposable variablesDisposable;
        private int nestedBulkEditingCounter;

        public SaiHistoryHandler(SmartScriptBase script, ISmartFactory smartFactory)
        {
            this.script = script;
            this.smartFactory = smartFactory;
            this.script.Events.CollectionChanged += Events_CollectionChanged;
            this.script.BulkEditingStarted += OnBulkEditingStarted;
            this.script.BulkEditingFinished += OnBulkEditingFinished;

            variablesDisposable = this.script.GlobalVariables.ToStream(true).Subscribe(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    e.Item.CommentChanged += OnGlobalVariableCommentChanged;
                    e.Item.VariableTypeChanged += OnGlobalVariableTypeChanged;
                    e.Item.NameChanged += OnGlobalVariableNameChanged;
                    e.Item.KeyChanged += OnGlobalVariableKeyChanged;
                    e.Item.EntryChanged += OnGlobalVariableEntryChanged;
                }
                else
                {
                    e.Item.CommentChanged -= OnGlobalVariableCommentChanged;
                    e.Item.VariableTypeChanged -= OnGlobalVariableTypeChanged;
                    e.Item.NameChanged -= OnGlobalVariableNameChanged;
                    e.Item.KeyChanged -= OnGlobalVariableKeyChanged;
                    e.Item.EntryChanged -= OnGlobalVariableEntryChanged;
                }
            });
            
            script.GlobalVariables.CollectionChanged += GlobalVariablesOnCollectionChanged;
            
            foreach (SmartEvent ev in this.script.Events)
                BindEvent(ev);
        }

        private void GlobalVariablesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (GlobalVariable gv in e.NewItems)
                    PushAction(new GlobalVariableAddedAction(script, gv, e.NewStartingIndex));
            }
            if (e.OldItems != null)
            {
                foreach (GlobalVariable gv in e.OldItems)
                    PushAction(new GlobalVariableRemovedAction(script, gv, e.OldStartingIndex));
            }
        }

        private void OnGlobalVariableKeyChanged(GlobalVariable variable, long old, long newValue)
        {
            PushAction(new GlobalVariableKeyChangedAction(variable, old, newValue));
        }
        
        private void OnGlobalVariableEntryChanged(GlobalVariable variable, uint old, uint newValue)
        {
            PushAction(new GlobalVariableEntryChangedAction(variable, old, newValue));
        }

        private void OnGlobalVariableTypeChanged(GlobalVariable variable, GlobalVariableType old, GlobalVariableType newValue)
        {
            PushAction(new GlobalVariableTypeChangedAction(variable, old, newValue));
        }

        private void OnGlobalVariableNameChanged(GlobalVariable variable, string old, string newValue)
        {
            PushAction(new GlobalVariableNameChangedAction(variable, old, newValue));
        }

        private void OnGlobalVariableCommentChanged(GlobalVariable variable, string? old, string? newValue)
        {
            PushAction(new GlobalVariableCommentChangedAction(variable, old, newValue));
        }

        public void Dispose()
        {
            variablesDisposable.Dispose();
            script.GlobalVariables.CollectionChanged -= GlobalVariablesOnCollectionChanged;
            script.Events.CollectionChanged -= Events_CollectionChanged;
            script.BulkEditingStarted -= OnBulkEditingStarted;
            script.BulkEditingFinished -= OnBulkEditingFinished;
        }

        private void UnbindSmartBase(SmartBaseElement element)
        {
            for (var i = 0; i < element.ParametersCount; ++i)
                element.GetParameter(i).OnValueChanged -= Parameter_OnValueChanged;
            
            for (var i = 0; i < element.FloatParametersCount; ++i)
                element.GetFloatParameter(i).OnValueChanged -= ParameterFloat_OnValueChange;
            
            for (var i = 0; i < element.StringParametersCount; ++i)
                element.GetStringParameter(i).OnValueChanged -= ParameterString_OnValueChanged;
        }

        private void BindSmartBase(SmartBaseElement element)
        {
            for (var i = 0; i < element.ParametersCount; ++i)
                element.GetParameter(i).OnValueChanged += Parameter_OnValueChanged;
            
            for (var i = 0; i < element.FloatParametersCount; ++i)
                element.GetFloatParameter(i).OnValueChanged += ParameterFloat_OnValueChange;
            
            for (var i = 0; i < element.StringParametersCount; ++i)
                element.GetStringParameter(i).OnValueChanged += ParameterString_OnValueChanged;
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
            smartEvent.TimerId.OnValueChanged -= Parameter_OnValueChanged;
            smartEvent.OnIdChanged -= SmartEventOnOnIdChanged;

            UnbindSmartBase(smartEvent);
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

            BindSmartBase(smartAction);
            BindSmartBase(smartAction.Source);
            BindSmartBase(smartAction.Target);

            smartAction.CommentParameter.OnValueChanged += ParameterString_OnValueChanged;
            smartAction.OnIdChanged += SmartActionOnOnIdChanged;
            smartAction.Source.OnIdChanged += SmartSourceOnOnIdChanged;
            smartAction.Target.OnIdChanged += SmartTargetOnOnIdChanged;

            smartAction.Source.OnConditionsChanged += SmartSourceOnConditionsChanged;
            smartAction.Target.OnConditionsChanged += SmartSourceOnConditionsChanged;
        }

        private void UnbindAction(SmartAction smartAction)
        {
            smartAction.Source.OnConditionsChanged -= SmartSourceOnConditionsChanged;
            smartAction.Target.OnConditionsChanged -= SmartSourceOnConditionsChanged;
            smartAction.Target.OnIdChanged -= SmartTargetOnOnIdChanged;
            smartAction.Source.OnIdChanged -= SmartSourceOnOnIdChanged;
            smartAction.OnIdChanged -= SmartActionOnOnIdChanged;
            smartAction.BulkEditingStarted -= OnBulkEditingStarted;
            smartAction.BulkEditingFinished -= OnBulkEditingFinished;
            
            UnbindSmartBase(smartAction);
            UnbindSmartBase(smartAction.Source);
            UnbindSmartBase(smartAction.Target);

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
            smartCondition.OnIndentChanged += OnConditionIndentChanged;
            
            BindSmartBase(smartCondition);

            smartCondition.Inverted.OnValueChanged += Parameter_OnValueChanged;
            smartCondition.ConditionTarget.OnValueChanged += Parameter_OnValueChanged;
        }

        private void OnConditionIndentChanged(SmartCondition condition, int old, int @new)
        {
            PushAction(new AnonymousHistoryAction("Change condition indent", () =>
            {
                condition.Indent = old;
            }, () =>
            {
                condition.Indent = @new;
            }));
        }

        private void UnbindCondition(SmartCondition smartCondition)
        {
            smartCondition.OnIndentChanged -= OnConditionIndentChanged;
            smartCondition.BulkEditingStarted -= OnBulkEditingStarted;
            smartCondition.BulkEditingFinished -= OnBulkEditingFinished;

            UnbindSmartBase(smartCondition);

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
            smartEvent.TimerId.OnValueChanged += Parameter_OnValueChanged;

            if (!smartEvent.IsBeginGroup) // BeginGroup is not a real event, its values are used for opened/closed state
                                          // so we shouldn't bind to it
                BindSmartBase(smartEvent);

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
        
        private void SmartSourceOnConditionsChanged(SmartSource source, IReadOnlyList<ICondition>? old, IReadOnlyList<ICondition>? nnew)
        {
            PushAction(new SourceTargetConditionsChanged(source, old, nnew));
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
            nestedBulkEditingCounter--;
            if (nestedBulkEditingCounter == 0)
                EndBulkEdit(editName.RemoveTags());
        }

        private void OnBulkEditingStarted()
        {
            if (nestedBulkEditingCounter == 0)
                StartBulkEdit();
            nestedBulkEditingCounter++;
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
        
        private class GlobalVariableAddedAction : IHistoryAction
        {
            private readonly int index;
            private readonly string readable;
            private readonly SmartScriptBase script;
            private readonly GlobalVariable variable;

            public GlobalVariableAddedAction(SmartScriptBase script, GlobalVariable variable, int index)
            {
                this.script = script;
                this.variable = variable;
                this.index = index;
                readable = variable.Readable;
            }

            public string GetDescription()
            {
                return "Added global variable " + readable.RemoveTags();
            }

            public void Redo()
            {
                variable.IsSelected = false;
                script.GlobalVariables.Insert(index, variable);
            }

            public void Undo()
            {
                script.GlobalVariables.Remove(variable);
            }
        }

        private class GlobalVariableRemovedAction : IHistoryAction
        {
            private readonly int index;
            private readonly SmartScriptBase script;
            private readonly GlobalVariable variable;
            private readonly string readable;

            public GlobalVariableRemovedAction(SmartScriptBase script, GlobalVariable variable, int index)
            {
                this.script = script;
                this.variable = variable;
                this.index = index;
                readable = variable.Readable;
            }

            public string GetDescription()
            {
                return "Removed global variable " + readable.RemoveTags();
            }

            public void Redo()
            {
                script.GlobalVariables.Remove(variable);
            }

            public void Undo()
            {
                variable.IsSelected = false;
                script.GlobalVariables.Insert(index, variable);
            }
        }
        
        private class EventAddedAction : IHistoryAction
        {
            private readonly int index;
            private readonly string readable;
            private readonly SmartScriptBase script;
            private readonly SmartEvent smartEvent;

            public EventAddedAction(SmartScriptBase script, SmartEvent smartEvent, int index)
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
            private readonly SmartScriptBase script;
            private readonly SmartEvent smartEvent;

            public EventRemovedAction(SmartScriptBase script, SmartEvent smartEvent, int index)
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

    public class SourceTargetConditionsChanged : IHistoryAction
    {
        private readonly SmartSource source;
        private readonly IReadOnlyList<ICondition>? old;
        private readonly IReadOnlyList<ICondition>? newConds;
        private readonly string readable;

        public SourceTargetConditionsChanged(SmartSource source, IReadOnlyList<ICondition>? old, IReadOnlyList<ICondition>? newConds)
        {
            this.source = source;
            this.old = old;
            this.newConds = newConds;
            this.readable = source.Readable.RemoveTags();
        }

        public string GetDescription()
        {
            return "Modified conditions of " + readable;
        }

        public void Redo()
        {
            source.Conditions = newConds;
        }

        public void Undo()
        {
            source.Conditions = old;
        }
    }
    
    public class ActionAddedAction : IHistoryAction
    {
        private readonly int index;
        private readonly SmartEvent parent;
        private readonly SmartAction smartAction;
        private readonly string readable;

        public ActionAddedAction(SmartEvent parent, SmartAction smartAction, int index)
        {
            this.parent = parent;
            this.smartAction = smartAction;
            this.index = index;
            this.readable = smartAction.Readable.RemoveTags();
        }

        public string GetDescription()
        {
            return "Added action " + readable;
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
    
    public class GlobalVariableNameChangedAction : IHistoryAction
    {
        private readonly string old;
        private readonly string newName;
        private readonly GlobalVariable variable;
        private readonly long id;

        public GlobalVariableNameChangedAction(GlobalVariable variable, string old, string newName)
        {
            this.variable = variable;
            this.old = old;
            this.newName = newName;
            this.id = variable.Key;
        }

        public string GetDescription() => "Global variable " + id + " name changed";

        public void Redo()
        {
            variable.Name = newName;
        }

        public void Undo()
        {
            variable.Name = old;
        }
    }
    
    public class GlobalVariableKeyChangedAction : IHistoryAction
    {
        private readonly long old;
        private readonly long newName;
        private readonly GlobalVariable variable;

        public GlobalVariableKeyChangedAction(GlobalVariable variable, long old, long newName)
        {
            this.variable = variable;
            this.old = old;
            this.newName = newName;
        }

        public string GetDescription() => "Global variable id changed";

        public void Redo()
        {
            variable.Key = newName;
        }

        public void Undo()
        {
            variable.Key = old;
        }
    }
    
    public class GlobalVariableEntryChangedAction : IHistoryAction
    {
        private readonly uint old;
        private readonly uint newName;
        private readonly GlobalVariable variable;

        public GlobalVariableEntryChangedAction(GlobalVariable variable, uint old, uint newName)
        {
            this.variable = variable;
            this.old = old;
            this.newName = newName;
        }

        public string GetDescription() => "Global variable entry changed";

        public void Redo()
        {
            variable.Entry = newName;
        }

        public void Undo()
        {
            variable.Entry = old;
        }
    }

    public class GlobalVariableTypeChangedAction : IHistoryAction
    {
        private readonly GlobalVariableType old;
        private readonly GlobalVariableType newName;
        private readonly GlobalVariable variable;
        private readonly long id;

        public GlobalVariableTypeChangedAction(GlobalVariable variable, GlobalVariableType old, GlobalVariableType newName)
        {
            this.variable = variable;
            this.old = old;
            this.newName = newName;
            this.id = variable.Key;
        }

        public string GetDescription() => "Global variable " + id + " type changed";

        public void Redo()
        {
            variable.VariableType = newName;
        }

        public void Undo()
        {
            variable.VariableType = old;
        }
    }
    
    public class GlobalVariableCommentChangedAction : IHistoryAction
    {
        private readonly string? old;
        private readonly string? newName;
        private readonly GlobalVariable variable;
        private readonly long id;

        public GlobalVariableCommentChangedAction(GlobalVariable variable, string? old, string? newName)
        {
            this.variable = variable;
            this.old = old;
            this.newName = newName;
            this.id = variable.Key;
        }

        public string GetDescription() => "Global variable " + id + " comment changed";

        public void Redo()
        {
            variable.Comment = newName;
        }

        public void Undo()
        {
            variable.Comment = old;
        }
    }
}