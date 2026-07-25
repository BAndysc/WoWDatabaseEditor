using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.Parameters.Models;

namespace WDE.CMangosConditions.ViewModels
{
    /// <summary>
    /// Records undo/redo actions for the unit condition editor: clause adds/removes,
    /// id/logic/op/value changes and variable changes.
    /// </summary>
    internal class UnitConditionHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly UnitConditionEditorViewModel viewModel;
        private readonly IUnitConditionClauseFactory factory;
        private readonly HashSet<UnitConditionClauseViewModel> wired = new();

        public UnitConditionHistoryHandler(UnitConditionEditorViewModel viewModel, IUnitConditionClauseFactory factory)
        {
            this.viewModel = viewModel;
            this.factory = factory;

            viewModel.Id.OnValueChanged += OnLongChanged;
            viewModel.IsOr.OnValueChanged += OnLongChanged;
            viewModel.Clauses.CollectionChanged += OnCollectionChanged;
            foreach (var clause in viewModel.Clauses)
                Wire(clause);
        }

        public void Dispose()
        {
            viewModel.Id.OnValueChanged -= OnLongChanged;
            viewModel.IsOr.OnValueChanged -= OnLongChanged;
            viewModel.Clauses.CollectionChanged -= OnCollectionChanged;
            foreach (var clause in new List<UnitConditionClauseViewModel>(wired))
                Unwire(clause);
        }

        private void Wire(UnitConditionClauseViewModel clause)
        {
            if (!wired.Add(clause))
                return;
            foreach (var holder in clause.Values())
                holder.OnValueChanged += OnLongChanged;
            clause.VariableChanged += OnVariableChanged;
        }

        private void Unwire(UnitConditionClauseViewModel clause)
        {
            if (!wired.Remove(clause))
                return;
            foreach (var holder in clause.Values())
                holder.OnValueChanged -= OnLongChanged;
            clause.VariableChanged -= OnVariableChanged;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is not ObservableCollection<UnitConditionClauseViewModel> collection)
                return;

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                int index = e.NewStartingIndex;
                foreach (UnitConditionClauseViewModel item in e.NewItems)
                {
                    Wire(item);
                    PushAction(new ClauseCollectionAction(collection, item, index++, isAdd: true));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                int index = e.OldStartingIndex;
                foreach (UnitConditionClauseViewModel item in e.OldItems)
                {
                    Unwire(item);
                    PushAction(new ClauseCollectionAction(collection, item, index, isAdd: false));
                }
            }
        }

        private void OnVariableChanged(UnitConditionClauseViewModel clause, int oldVariable, int newVariable)
        {
            if (oldVariable != newVariable)
                PushAction(new UnitConditionVariableChangedAction(factory, clause, oldVariable, newVariable));
        }

        private void OnLongChanged(ParameterValueHolder<long> holder, long old, long @new) =>
            PushAction(new HolderValueChangedAction<long>(holder, old, @new));
    }

    internal class ClauseCollectionAction : IHistoryAction
    {
        private readonly ObservableCollection<UnitConditionClauseViewModel> collection;
        private readonly UnitConditionClauseViewModel clause;
        private readonly int index;
        private readonly bool isAdd;

        public ClauseCollectionAction(ObservableCollection<UnitConditionClauseViewModel> collection,
            UnitConditionClauseViewModel clause, int index, bool isAdd)
        {
            this.collection = collection;
            this.clause = clause;
            this.index = index;
            this.isAdd = isAdd;
        }

        public void Undo()
        {
            if (isAdd)
                collection.Remove(clause);
            else
                collection.Insert(Math.Min(index, collection.Count), clause);
        }

        public void Redo()
        {
            if (isAdd)
                collection.Insert(Math.Min(index, collection.Count), clause);
            else
                collection.Remove(clause);
        }

        public string GetDescription() => isAdd ? "Clause added" : "Clause removed";
    }

    internal class UnitConditionVariableChangedAction : IHistoryAction
    {
        private readonly IUnitConditionClauseFactory factory;
        private readonly UnitConditionClauseViewModel clause;
        private readonly int old;
        private readonly int @new;

        public UnitConditionVariableChangedAction(IUnitConditionClauseFactory factory,
            UnitConditionClauseViewModel clause, int old, int @new)
        {
            this.factory = factory;
            this.clause = clause;
            this.old = old;
            this.@new = @new;
        }

        public void Undo() => factory.Update(old, clause);
        public void Redo() => factory.Update(@new, clause);
        public string GetDescription() => $"Clause variable changed to {@new}";
    }
}
