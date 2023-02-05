using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;
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
        disposable = viewModel.EntitiesObservable[0].ToStream(false).SubscribeAction(e =>
        {
            if (e.Type == CollectionEventType.Add)
            {
                e.Item.FieldValueChanged += FieldValueChanged;
                e.Item.OnConditionsChanged += OnConditionsChanged;
                PushAction(new DatabaseEntityAddedByIndexHistoryAction(e.Item, e.Index, viewModel));
            }
            else if (e.Type == CollectionEventType.Remove)
            {
                PushAction(new DatabaseEntityRemovedByIndexHistoryAction(e.Item, e.Index, viewModel));
                e.Item.FieldValueChanged -= FieldValueChanged;
                e.Item.OnConditionsChanged -= OnConditionsChanged;
            }
        });
    }

    private void FieldValueChanged(DatabaseEntity entity, string columnName, Action<IValueHolder> undo, Action<IValueHolder> redo)
    {
        var index = viewModel.Entities.IndexOf(entity);
        if (entity.Phantom || !viewModel.TableDefinition.GroupByKeys.Contains(columnName))
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


public class DatabaseEntityAddedByIndexHistoryAction : IHistoryAction
{
    private readonly DatabaseEntity entity;
    private readonly int index;
    private readonly ViewModelBase viewModel;
    private readonly DatabaseKey actualKey;

    public DatabaseEntityAddedByIndexHistoryAction(DatabaseEntity entity, int index,
        ViewModelBase viewModel)
    {
        this.entity = entity;
        this.index = index;
        this.viewModel = viewModel;
        actualKey = entity.GenerateKey(viewModel.TableDefinition);
    }
        
    public void Undo()
    {
        viewModel.ForceRemoveEntity(viewModel.Entities[index]);
    }

    public void Redo()
    {
        viewModel.ForceInsertEntity(entity, index);
    }

    public string GetDescription()
    {
        return $"Entity {actualKey} added";
    }
}
    
public class DatabaseEntityRemovedByIndexHistoryAction : IHistoryAction
{
    private readonly DatabaseEntity entity;
    private readonly int index;
    private readonly ViewModelBase viewModel;
    private readonly DatabaseKey actualKey;

    public DatabaseEntityRemovedByIndexHistoryAction(DatabaseEntity entity, int index, ViewModelBase viewModel)
    {
        this.entity = entity;
        this.index = index;
        this.viewModel = viewModel;
        actualKey = entity.GenerateKey(viewModel.TableDefinition);
    }
        
    public void Undo()
    {
        viewModel.ForceInsertEntity(entity, index, true);
    }

    public void Redo()
    {
        viewModel.ForceRemoveEntity(viewModel.Entities[index]);
    }

    public string GetDescription()
    {
        return $"Entity {actualKey} removed";
    }
}