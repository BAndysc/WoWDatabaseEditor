using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.History.SingleRow;

public class SingleRowTableEditorHistoryHandler : HistoryHandler, IDisposable
{
    private readonly SingleRowDbTableEditorViewModel viewModel;
    private IDisposable? disposable;

    private HashSet<string> keys;

    public SingleRowTableEditorHistoryHandler(SingleRowDbTableEditorViewModel viewModel)
    {
        this.viewModel = viewModel;
        keys = new HashSet<string>(viewModel.TableDefinition.GroupByKeys);
        BindTableData();
    }

    public IDisposable BulkEdit(string name) => WithinBulk(name);
        
    public void Dispose()
    {
        UnbindTableData();
    }

    private void BindTableData()
    {
        disposable = viewModel.Entities.ToStream(false).SubscribeAction(e =>
        {
            if (e.Type == CollectionEventType.Add)
            {
                e.Item.FieldValueChanged += FieldValueChanged;
                e.Item.OnConditionsChanged += OnConditionsChanged;
                PushAction(new DatabaseEntityAddedHistoryAction(e.Item, e.Index, viewModel));
            }
            else if (e.Type == CollectionEventType.Remove)
            {
                PushAction(new DatabaseEntityRemovedHistoryAction(e.Item, e.Index, viewModel));
                e.Item.FieldValueChanged -= FieldValueChanged;
                e.Item.OnConditionsChanged -= OnConditionsChanged;
            }
        });
    }

    private void FieldValueChanged(DatabaseEntity entity, string columnName, Action<IValueHolder> undo, Action<IValueHolder> redo)
    {
        var index = viewModel.Entities.IndexOf(entity);
        PushAction(new SingleRowFieldCellValueChangedAction(viewModel, index, columnName, undo, redo));
    }

    private void OnConditionsChanged(DatabaseEntity entity, IReadOnlyList<ICondition>? old, IReadOnlyList<ICondition>? @new)
    {
        PushAction(new DatabaseEntityConditionsChangedHistoryAction(entity, old, @new, viewModel));
    }

    private void OnAction(IHistoryAction action)
    {
        if (action is DatabaseFieldWithKeyHistoryAction fieldChanged)
        {
            if (!fieldChanged.Key.IsPhantomKey && keys.Contains(fieldChanged.Property))
                return;
        }
        PushAction(action);
    }
        
    private void UnbindTableData()
    {
        foreach (var e in viewModel.Entities)
            e.OnAction -= OnAction;
        disposable?.Dispose();
        disposable = null;
    }
}

public class SingleRowFieldCellValueChangedAction : IHistoryAction
{
    private readonly SingleRowDbTableEditorViewModel viewModel;
    private readonly int index;
    private readonly string columnName;
    private readonly Action<IValueHolder> undo;
    private readonly Action<IValueHolder> redo;

    public SingleRowFieldCellValueChangedAction(SingleRowDbTableEditorViewModel viewModel, int index, string columnName, Action<IValueHolder> undo, Action<IValueHolder> redo)
    {
        this.viewModel = viewModel;
        this.index = index;
        this.columnName = columnName;
        this.undo = undo;
        this.redo = redo;
    }

    public void Undo()
    {
        undo(viewModel.Entities[index].GetCell(columnName)!.CurrentValue);
    }

    public void Redo()
    {
        redo(viewModel.Entities[index].GetCell(columnName)!.CurrentValue);
    }

    public string GetDescription()
    {
        return "Changed " + columnName;
    }
}