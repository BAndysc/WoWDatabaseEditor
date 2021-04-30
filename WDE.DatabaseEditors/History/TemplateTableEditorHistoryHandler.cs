using System;
using WDE.Common.History;
using WDE.DatabaseEditors.ViewModels;
using WDE.DatabaseEditors.ViewModels.Template;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.History
{
    public class TemplateTableEditorHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly TemplateDbTableEditorViewModel viewModel;
        private IDisposable? disposable;
        
        public TemplateTableEditorHistoryHandler(TemplateDbTableEditorViewModel viewModel)
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
        }

        private void UnbindTableData()
        {
            disposable?.Dispose();
            disposable = null;
        }
    }
}