using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.Common.Types;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.History;

namespace WDE.DatabaseEditors.Models
{
    /*public class DatabaseMultiRecordTableData : IDatabaseTableData, INotifyRowChanged
    {
        public string TableName { get; }
        public string DbTableName { get; }
        public string TableIndexFieldName { get; }
        public string TableIndexValue { get; }
        public List<IDatabaseColumn> Columns { get; }
        public ObservableCollection<Dictionary<string, IDatabaseField>> Rows { get; }
        public ObservableCollection<ColumnDescriptor> RowsDescriptors { get; }

        public DatabaseMultiRecordTableData(string tableName, string dbTableName, string tableIndexFieldName, 
            string tableIndexValue, List<IDatabaseColumn> columns)
        {
            TableName = tableName;
            DbTableName = dbTableName;
            TableIndexFieldName = tableIndexFieldName;
            TableIndexValue = tableIndexValue;
            Columns = columns;
            Rows = new ObservableCollection<Dictionary<string, IDatabaseField>>();
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
                Rows.Add(new Dictionary<string, IDatabaseField>());
                
                foreach (var column in Columns)
                    Rows[i].Add(column.ColumnName, column.Fields[i]);
            }
            
            foreach(var column in Columns)
                RowsDescriptors.Add(new ColumnDescriptor(column.ColumnName, $"[{column.ColumnName}]"));
        }
        
        public void AddRow(IDatabaseFieldFactory creator)
        {
            var values = new List<IDatabaseField>(Columns.Count);
            var dict = new Dictionary<string, IDatabaseField>();
            
            foreach (var column in Columns)
            {
                var field = creator.CreateField(column.FieldDataSource, column.GetDefaultValue(), column);
                values.Add(field);
                column.Fields.Add(field);
                dict.Add(field.FieldMetaData.Name, field);
            }
            
            Rows.Add(dict);
            OnRowChanged.Invoke(this, new NotifyRowChangedEventArgs(Columns[0].Fields.Count - 1, values, null));
        }

        public void DeleteRow(int at)
        {
            var removedRows = new List<IDatabaseField>(Columns.Count);
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

        public void FillToRow(IDatabaseFieldFactory creator, int row)
        {
            foreach (var column in Columns)
            {
                while (column.Fields.Count < (row + 1))
                    column.Fields.Add(creator.CreateField(column.FieldDataSource, column.GetDefaultValue(), column));
            }
        }

        public event EventHandler<NotifyRowChangedEventArgs> OnRowChanged = delegate {  };
    }*/
}