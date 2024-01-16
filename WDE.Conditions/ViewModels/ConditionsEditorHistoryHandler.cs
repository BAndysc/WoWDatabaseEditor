using WDE.Common.History;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;

namespace WDE.Conditions.ViewModels
{
    internal class ConditionsEditorHistoryHandler : HistoryHandler, System.IDisposable
    {
        private readonly IConditionsFactory conditionsFactory;
        private System.IDisposable conditionsSub;
        
        public ConditionsEditorHistoryHandler(ConditionsEditorViewModel viewModel, IConditionsFactory conditionsFactory)
        {
            this.conditionsFactory = conditionsFactory;
            conditionsSub = viewModel.Conditions.ToStream(false).SubscribeAction(a =>
            {
                if (a.Type == CollectionEventType.Add)
                {
                    foreach (var val in  a.Item.Values())
                        val.OnValueChanged += OnChanged;
                    foreach (var val in  a.Item.StringValues())
                        val.OnValueChanged += OnStringChanged;
                    a.Item.ConditionChanged += OnConditionChanged;
                    PushAction(new ConditionAddedAction(viewModel, a.Item, a.Index));
                }
                else
                {
                    a.Item.ConditionChanged -= OnConditionChanged;
                    foreach (var val in  a.Item.Values())
                        val.OnValueChanged -= OnChanged;
                    foreach (var val in  a.Item.StringValues())
                        val.OnValueChanged -= OnStringChanged;
                    PushAction(new ConditionRemovedAction(viewModel, a.Item, a.Index));
                }
            });
        }

        private void OnConditionChanged(ConditionViewModel condition, int oldId, int newId)
        {
            PushAction(new ConditionTypeChangedAction(conditionsFactory, condition, oldId, newId));
        }

        private void OnChanged(ParameterValueHolder<long> param, long old, long nnew)
        {
            PushAction(new ParameterValueChangedAction<long>(param, old, nnew));
        }

        private void OnStringChanged(ParameterValueHolder<string> param, string old, string nnew)
        {
            PushAction(new ParameterValueChangedAction<string>(param, old, nnew));
        }

        public System.IDisposable BulkEdit(string name)
        {
            StartBulkEdit();
            return new DelegateDisposable(() => EndBulkEdit(name));
        }

        public void Dispose()
        {
            conditionsSub?.Dispose();
        }

        private class DelegateDisposable : System.IDisposable
        {
            private System.Action action;
            public DelegateDisposable(System.Action action)
            {
                this.action = action;
            }
            
            public void Dispose()
            {
                action();
            }
        }
    }

    internal class ConditionAddedAction : IHistoryAction
    {
        private readonly ConditionsEditorViewModel vm;
        private readonly ConditionViewModel condition;
        private readonly int index;

        public ConditionAddedAction(ConditionsEditorViewModel vm, ConditionViewModel condition, int index)
        {
            this.vm = vm;
            this.condition = condition;
            this.index = index;
        }
        
        public void Undo()
        {
            vm.Conditions.RemoveAt(index);   
        }

        public void Redo()
        {
            vm.Conditions.Insert(index, condition);
        }

        public string GetDescription() => "Condition added";
    }
    
    internal class ConditionRemovedAction : IHistoryAction
    {
        private readonly ConditionsEditorViewModel vm;
        private readonly ConditionViewModel condition;
        private readonly int index;

        public ConditionRemovedAction(ConditionsEditorViewModel vm, ConditionViewModel condition, int index)
        {
            this.vm = vm;
            this.condition = condition;
            this.index = index;
        }
        
        public void Undo()
        {
            vm.Conditions.Insert(index, condition);
        }

        public void Redo()
        {
            vm.Conditions.RemoveAt(index);
        }

        public string GetDescription() => "Condition removed";
    }
    
    internal class ParameterValueChangedAction<T> : IHistoryAction where T : notnull
    {
        private readonly ParameterValueHolder<T> holder;
        private readonly T old;
        private readonly T nnew;
        private readonly string name;

        public ParameterValueChangedAction(ParameterValueHolder<T> holder, T old, T nnew)
        {
            this.holder = holder;
            name = holder.Name;
            this.old = old;
            this.nnew = nnew;
        }
        
        public void Undo()
        {
            holder.Value = old;
        }

        public void Redo()
        {
            holder.Value = nnew;
        }

        public string GetDescription() => $"{name} changed to {nnew}";
    }
    
    internal class ConditionTypeChangedAction : IHistoryAction
    {
        private readonly IConditionsFactory conditionsFactory;
        private readonly ConditionViewModel condition;
        private readonly int old;
        private readonly int nnew;

        public ConditionTypeChangedAction(IConditionsFactory conditionsFactory, ConditionViewModel condition, int old, int nnew)
        {
            this.conditionsFactory = conditionsFactory;
            this.condition = condition;
            this.old = old;
            this.nnew = nnew;
        }
        
        public void Undo()
        {
            conditionsFactory.Update(old, condition);
        }

        public void Redo()
        {
            conditionsFactory.Update(nnew, condition);
        }

        public string GetDescription() => $"Condition type changed to {nnew}";
    }
}