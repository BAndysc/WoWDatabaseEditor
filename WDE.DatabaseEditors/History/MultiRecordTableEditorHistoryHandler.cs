using System;
using System.Collections.Generic;
using WDE.Common.History;
using WDE.DatabaseEditors.Models;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.History
{
    public class MultiRecordTableEditorHistoryHandler : HistoryHandler, IDisposable, IDatabaseFieldHistoryActionReceiver
    {
        private readonly DatabaseMultiRecordTableData tableData;
        private List<System.IDisposable> disposables = new();
        
        public MultiRecordTableEditorHistoryHandler(DatabaseMultiRecordTableData tableData)
        {
            this.tableData = tableData;
            BindTableData();
        }

        public void Dispose()
        {
            foreach (var d in disposables)
                d.Dispose();
            disposables.Clear();
                
            tableData.OnRowChanged -= TableDataOnOnRowChanged;
        }

        public void RegisterAction(IHistoryAction action) => PushAction(action);

        private void BindTableData()
        {
            foreach (var category in tableData.Columns)
            {
                disposables.Add(category.Fields.ToStream().Subscribe(item =>
                {
                    if (item.Type == CollectionEventType.Add)
                    {
                        if (item.Item is IDatabaseTableHistoryActionSource actionSource)
                            actionSource.RegisterActionReceiver(this);
                    }
                    else if (item.Type == CollectionEventType.Remove)
                    {
                        if (item.Item is IDatabaseTableHistoryActionSource actionSource)
                            actionSource.RegisterActionReceiver(this);
                    }
                }));
            }
            
            tableData.OnRowChanged += TableDataOnOnRowChanged;
        }

        private void TableDataOnOnRowChanged(object? sender, NotifyRowChangedEventArgs e)
        {
            PushAction(new TableRowsChangedHistoryAction(tableData, e.Row, e.NewValues != null, 
                e.NewValues ?? e.OldValues! ));
        }
    }

    internal class TableRowsChangedHistoryAction : IHistoryAction
    {
        private readonly DatabaseMultiRecordTableData tableData;
        private readonly int row;
        private readonly bool isInsertAction;
        private readonly List<IDatabaseField> values;

        public TableRowsChangedHistoryAction(DatabaseMultiRecordTableData tableData, int row,  bool isInsertAction,
            List<IDatabaseField> values)
        {
            this.tableData = tableData;
            this.row = row;
            this.isInsertAction = isInsertAction;
            this.values = values;
        }

        public void Undo() => Change(!isInsertAction);

        public void Redo() => Change(isInsertAction);

        private void Change(bool insert)
        {
            if (insert)
            {
                if (values.Count != tableData.Columns.Count)
                    return;
                
                for (int i = 0; i < values.Count; ++i)
                    tableData.Columns[i].Fields.Insert(row, values[i]);

                var dict = new Dictionary<string, IDatabaseField>();
                foreach (var field in values)
                    dict.Add(field.FieldName, field);
                tableData.Rows.Insert(row, dict);
            }
            else
            {
                foreach(var column in tableData.Columns)
                    column.Fields.RemoveAt(row);
                
                tableData.Rows.RemoveAt(row);
            }
        }

        public string GetDescription() => $"{(isInsertAction ? "Added" : "Removed")} row";
    }
}