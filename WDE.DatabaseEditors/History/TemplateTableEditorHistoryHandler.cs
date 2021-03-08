using System;
using System.ComponentModel;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.History
{
    public class TemplateTableEditorHistoryHandler : HistoryHandler, IDisposable, IDbFieldHistoryActionReceiver
    {
        private readonly DbTableData tableData;

        public TemplateTableEditorHistoryHandler(DbTableData tableData)
        {
            this.tableData = tableData;
            BindTableData();
        }

        public void Dispose()
        {
            UnbindTableData();
        }

        public void RegisterAction(IHistoryAction action) => PushAction(action);

        private void BindTableData()
        {
            foreach (var category in tableData.Categories)
            {
                foreach (var field in category.Fields)
                {
                    if (field is IDbTableHistoryActionSource actionSource)
                        actionSource.RegisterActionReceiver(this);
                }
            }
        }

        private void UnbindTableData()
        {
            foreach (var category in tableData.Categories)
            {
                foreach (var field in category.Fields)
                {
                    if (field is IDbTableHistoryActionSource actionSource)
                        actionSource.UnregisterActionReceiver();
                }
            }
        }
    }
}