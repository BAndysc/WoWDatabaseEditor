using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.History
{
    public class MultiRowTableEditorHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly MultiRowDbTableEditorViewModel viewModel;
        private IDisposable? disposable;
        
        public MultiRowTableEditorHistoryHandler(MultiRowDbTableEditorViewModel viewModel)
        {
            this.viewModel = viewModel;
            BindTableData();
        }

        public IDisposable BulkEdit(string name) => WithinBulk(name);
        
        public void Dispose()
        {
            UnbindTableData();
        }

        private Dictionary<ObservableCollection<DatabaseEntity>, System.IDisposable> innerCollectionsToDisposable = new();

        private void BindTableData()
        {
            disposable = viewModel.EntitiesObservable.ToStream(true).SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    var disp = e.Item.ToStream(true).SubscribeAction(inner =>
                    {
                        if (inner.Type == CollectionEventType.Add)
                        {
                            inner.Item.FieldValueChanged += FieldValueChanged;
                            inner.Item.OnConditionsChanged += OnConditionsChanged;
                        }
                        else if (inner.Type == CollectionEventType.Remove)
                        {
                            inner.Item.OnConditionsChanged -= OnConditionsChanged;
                            inner.Item.FieldValueChanged -= FieldValueChanged;
                        }
                    });
                    innerCollectionsToDisposable[e.Item] = disp;
                }
                else if (e.Type == CollectionEventType.Remove)
                {
                    if (innerCollectionsToDisposable.TryGetValue(e.Item, out var disp))
                    {
                        disp.Dispose();
                        innerCollectionsToDisposable.Remove(e.Item);
                    }
                }
            });
            viewModel.OnKeyDeleted += ViewModelOnDeleteQuery;
            viewModel.OnKeyAdded += ViewModelOnKeyAdded;
            viewModel.OnDeletedQuery += ViewModelOnDeletedQuery;
        }

        private void FieldValueChanged(DatabaseEntity entity, string columnName, Action<IValueHolder> undo, Action<IValueHolder> redo)
        {
            var index = viewModel.Entities.GetIndex(entity);
            PushAction(new AnonymousHistoryAction($"{columnName} changed", () =>
            {
                var field = viewModel.EntitiesObservable[index.group][index.index].GetCell(columnName);
                undo(field!.CurrentValue);
            }, () =>
            {
                var field = viewModel.EntitiesObservable[index.group][index.index].GetCell(columnName);
                redo(field!.CurrentValue);
            }));
        }

        private void OnConditionsChanged(DatabaseEntity entity, IReadOnlyList<ICondition>? old, IReadOnlyList<ICondition>? @new)
        {
            PushAction(new DatabaseEntityConditionsChangedHistoryAction(entity, old, @new, viewModel));
        }
        
        private void ViewModelOnDeletedQuery(DatabaseEntity obj)
        {
            PushAction(new DatabaseExecuteDeleteHistoryAction(viewModel, obj));
        }

        private void ViewModelOnKeyAdded(DatabaseKey key)
        {
            PushAction(new DatabaseKeyAddedHistoryAction(viewModel, key));
        }

        private void ViewModelOnDeleteQuery(DatabaseEntity obj)
        {
            PushAction(new DatabaseKeyRemovedHistoryAction(viewModel, obj.GenerateKey(viewModel.TableDefinition)));
        }

        private void UnbindTableData()
        {
            viewModel.OnDeletedQuery -= ViewModelOnDeletedQuery;
            viewModel.OnKeyAdded -= ViewModelOnKeyAdded;
            viewModel.OnKeyDeleted -= ViewModelOnDeleteQuery;
            disposable?.Dispose();
            disposable = null;
        }
    }
}