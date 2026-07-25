using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.Parameters.Models;

namespace WDE.CMangosConditions.ViewModels
{
    /// <summary>
    /// Records undo/redo actions for the whole condition tree: node adds/removes in any
    /// Children collection (and Roots), parameter/flag/comment value changes and type changes.
    /// </summary>
    internal class MangosConditionsHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly MangosConditionsEditorViewModel viewModel;
        private readonly IMangosConditionsFactory factory;
        private readonly HashSet<MangosConditionViewModel> wired = new();

        public MangosConditionsHistoryHandler(MangosConditionsEditorViewModel viewModel, IMangosConditionsFactory factory)
        {
            this.viewModel = viewModel;
            this.factory = factory;

            viewModel.Roots.CollectionChanged += OnCollectionChanged;
            foreach (var root in viewModel.Roots)
                Wire(root, record: false);
        }

        public void Dispose()
        {
            viewModel.Roots.CollectionChanged -= OnCollectionChanged;
            foreach (var node in new List<MangosConditionViewModel>(wired))
                Unwire(node);
        }

        private void Wire(MangosConditionViewModel node, bool record)
        {
            if (!wired.Add(node))
                return;
            foreach (var holder in node.Values())
                holder.OnValueChanged += OnLongChanged;
            foreach (var holder in node.StringValues())
                holder.OnValueChanged += OnStringChanged;
            node.ConditionChanged += OnConditionTypeChanged;
            node.Children.CollectionChanged += OnCollectionChanged;
            foreach (var child in node.Children)
                Wire(child, record);
        }

        private void Unwire(MangosConditionViewModel node)
        {
            if (!wired.Remove(node))
                return;
            foreach (var holder in node.Values())
                holder.OnValueChanged -= OnLongChanged;
            foreach (var holder in node.StringValues())
                holder.OnValueChanged -= OnStringChanged;
            node.ConditionChanged -= OnConditionTypeChanged;
            node.Children.CollectionChanged -= OnCollectionChanged;
            foreach (var child in node.Children)
                Unwire(child);
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is not ObservableCollection<MangosConditionViewModel> collection)
                return;

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                int index = e.NewStartingIndex;
                foreach (MangosConditionViewModel item in e.NewItems)
                {
                    Wire(item, record: true);
                    PushAction(new TreeNodeCollectionAction(collection, item, index++, isAdd: true));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                int index = e.OldStartingIndex;
                foreach (MangosConditionViewModel item in e.OldItems)
                {
                    Unwire(item);
                    PushAction(new TreeNodeCollectionAction(collection, item, index, isAdd: false));
                }
            }
        }

        private void OnConditionTypeChanged(MangosConditionViewModel node, int oldType, int newType)
        {
            if (oldType != newType)
                PushAction(new ConditionTypeChangedAction(factory, node, oldType, newType));
        }

        private void OnLongChanged(ParameterValueHolder<long> holder, long old, long @new) =>
            PushAction(new HolderValueChangedAction<long>(holder, old, @new));

        private void OnStringChanged(ParameterValueHolder<string> holder, string old, string @new) =>
            PushAction(new HolderValueChangedAction<string>(holder, old, @new));

        public IDisposable BulkEdit(string name)
        {
            StartBulkEdit();
            return new DelegateDisposable(() => EndBulkEdit(name));
        }

        private class DelegateDisposable : IDisposable
        {
            private readonly Action action;
            public DelegateDisposable(Action action) => this.action = action;
            public void Dispose() => action();
        }
    }

    internal class TreeNodeCollectionAction : IHistoryAction
    {
        private readonly ObservableCollection<MangosConditionViewModel> collection;
        private readonly MangosConditionViewModel node;
        private readonly int index;
        private readonly bool isAdd;

        public TreeNodeCollectionAction(ObservableCollection<MangosConditionViewModel> collection,
            MangosConditionViewModel node, int index, bool isAdd)
        {
            this.collection = collection;
            this.node = node;
            this.index = index;
            this.isAdd = isAdd;
        }

        public void Undo()
        {
            if (isAdd)
                collection.Remove(node);
            else
                collection.Insert(Math.Min(index, collection.Count), node);
        }

        public void Redo()
        {
            if (isAdd)
                collection.Insert(Math.Min(index, collection.Count), node);
            else
                collection.Remove(node);
        }

        public string GetDescription() => isAdd ? "Condition added" : "Condition removed";
    }

    internal class HolderValueChangedAction<T> : IHistoryAction where T : notnull
    {
        private readonly ParameterValueHolder<T> holder;
        private readonly T old;
        private readonly T @new;
        private readonly string name;

        public HolderValueChangedAction(ParameterValueHolder<T> holder, T old, T @new)
        {
            this.holder = holder;
            name = holder.Name;
            this.old = old;
            this.@new = @new;
        }

        public void Undo() => holder.Value = old;
        public void Redo() => holder.Value = @new;
        public string GetDescription() => $"{name} changed to {@new}";
    }

    internal class ConditionTypeChangedAction : IHistoryAction
    {
        private readonly IMangosConditionsFactory factory;
        private readonly MangosConditionViewModel node;
        private readonly int old;
        private readonly int @new;

        public ConditionTypeChangedAction(IMangosConditionsFactory factory, MangosConditionViewModel node, int old, int @new)
        {
            this.factory = factory;
            this.node = node;
            this.old = old;
            this.@new = @new;
        }

        public void Undo() => factory.Update(old, node);
        public void Redo() => factory.Update(@new, node);
        public string GetDescription() => $"Condition type changed to {@new}";
    }
}
