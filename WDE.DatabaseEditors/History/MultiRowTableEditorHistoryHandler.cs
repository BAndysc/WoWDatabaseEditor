using System;
using WDE.Common.History;
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

        public void Dispose()
        {
            UnbindTableData();
        }

        private void BindTableData()
        {
            disposable = viewModel.Entities.ToStream().SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    e.Item.OnAction += PushAction;
                    PushAction(new DatabaseEntityAddedHistoryAction(e.Item, e.Index, viewModel));
                }
                else if (e.Type == CollectionEventType.Remove)
                {
                    PushAction(new DatabaseEntityRemovedHistoryAction(e.Item, e.Index, viewModel));
                    e.Item.OnAction -= PushAction;
                }
            });
            viewModel.OnKeyDeleted += ViewModelOnDeleteQuery;
            viewModel.OnKeyAdded += ViewModelOnKeyAdded;
            viewModel.OnDeletedQuery += ViewModelOnDeletedQuery;
        }

        private void ViewModelOnDeletedQuery(DatabaseEntity obj)
        {
            PushAction(new DatabaseExecuteDeleteHistoryAction(viewModel, obj));
        }

        private void ViewModelOnKeyAdded(uint key)
        {
            PushAction(new DatabaseKeyAddedHistoryAction(viewModel, key));
        }

        private void ViewModelOnDeleteQuery(DatabaseEntity obj)
        {
            PushAction(new DatabaseKeyRemovedHistoryAction(viewModel, obj.Key));
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