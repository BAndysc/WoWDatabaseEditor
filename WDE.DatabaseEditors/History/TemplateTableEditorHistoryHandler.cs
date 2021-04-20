using System;
using System.ComponentModel;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.History
{
    public class TemplateTableEditorHistoryHandler : HistoryHandler, IDisposable, IDatabaseFieldHistoryActionReceiver
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

        public void RegisterAction(IHistoryAction action) => PushAction(action);

        private void BindTableData()
        {
            disposable = viewModel.Entities.ToStream().SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    e.Item.OnAction += PushAction;
                }
                else if (e.Type == CollectionEventType.Remove)
                {
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