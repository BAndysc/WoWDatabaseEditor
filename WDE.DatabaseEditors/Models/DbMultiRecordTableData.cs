using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.Common.Types;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.History;

namespace WDE.DatabaseEditors.Models
{
    public class DbMultiRecordTableData : IDbTableData, INotifyRowChanged
    {
        public string TableName { get; }
        public string DbTableName { get; }
        public string TableIndexFieldName { get; }
        public string TableIndexValue { get; }
        public List<IDbTableColumn> Columns { get; }
        public ObservableCollection<Dictionary<string, IDbTableField>> Rows { get; }
        public ObservableCollection<ColumnDescriptor> RowsDescriptors { get; }

        public DbMultiRecordTableData(string tableName, string dbTableName, string tableIndexFieldName, 
            string tableIndexValue, List<IDbTableColumn> columns)
        {
            TableName = tableName;
            DbTableName = dbTableName;
            TableIndexFieldName = tableIndexFieldName;
            TableIndexValue = tableIndexValue;
            Columns = columns;
            Rows = new ObservableCollection<Dictionary<string, IDbTableField>>();
            RowsDescriptors = new ObservableCollection<ColumnDescriptor>();
            InitRows();
        }

        public void InitRows()
        {
            Rows.Clear();
            RowsDescriptors.Clear();
            
            var amountOfRows = Columns[0].Fields.Count;
            for (int i = 0; i < amountOfRows; ++i)
            {
                Rows.Add(new Dictionary<string, IDbTableField>());
                
                foreach (var column in Columns)
                    Rows[i].Add(column.ColumnName, column.Fields[i]);
            }
            
            foreach(var column in Columns)
                RowsDescriptors.Add(new ColumnDescriptor(column.ColumnName, $"[{column.ColumnName}]"));
        }
        
        public void AddRow(IDbTableFieldFactory creator)
        {
            var values = new List<IDbTableField>(Columns.Count);
            var dict = new Dictionary<string, IDbTableField>();
            
            foreach (var column in Columns)
            {
                var field = creator.CreateField(column.FieldDataSource, column.GetDefaultValue(), column);
                values.Add(field);
                column.Fields.Add(field);
                dict.Add(field.FieldName, field);
            }
            
            Rows.Add(dict);
            OnRowChanged.Invoke(this, new NotifyRowChangedEventArgs(Columns[0].Fields.Count - 1, values, null));
        }

        public void DeleteRow(int at)
        {
            var removedRows = new List<IDbTableField>(Columns.Count);
            foreach (var column in Columns)
            {
                if (at < column.Fields.Count)
                {
                    removedRows.Add(column.Fields[at]);
                    column.Fields.RemoveAt(at);
                }
            }
            Rows.RemoveAt(at);
            OnRowChanged.Invoke(this, new NotifyRowChangedEventArgs(at, null, removedRows));
        }

        public void FillToRow(IDbTableFieldFactory creator, int row)
        {
            foreach (var column in Columns)
            {
                while (column.Fields.Count < (row + 1))
                    column.Fields.Add(creator.CreateField(column.FieldDataSource, column.GetDefaultValue(), column));
            }
        }

        public event EventHandler<NotifyRowChangedEventArgs> OnRowChanged = delegate {  };
    }
}