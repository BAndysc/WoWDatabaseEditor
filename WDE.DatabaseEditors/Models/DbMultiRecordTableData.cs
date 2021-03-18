using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WDE.DatabaseEditors.Data;
using WDE.Common.Types;

namespace WDE.DatabaseEditors.Models
{
    public class DbMultiRecordTableData : IDbTableData, IDbTableRowHolder
    {
        public string TableName { get; }
        public string DbTableName { get; }
        public string TableIndexFieldName { get; }
        public string TableIndexValue { get; }
        public string TableDescription { get; }
        public List<IDbTableColumn> Columns { get; }
        public ObservableCollection<IDbTableRow> Rows { get; }
        public ObservableCollection<ColumnDescriptor> ColumnDescriptors { get; }
        
        public DbMultiRecordTableData(string tableName, string dbTableName, string tableIndexFieldName, 
            string tableIndexValue, string tableDescription, List<IDbTableColumn> columns)
        {
            TableName = tableName;
            DbTableName = dbTableName;
            TableIndexFieldName = tableIndexFieldName;
            TableIndexValue = tableIndexValue;
            TableDescription = tableDescription;
            Columns = columns;
            Rows = new ObservableCollection<IDbTableRow>();
            InitRows();
            ColumnDescriptors = new ObservableCollection<ColumnDescriptor>(Columns.Select(c =>
                new ColumnDescriptor(c.ColumnName,
                    "Fields")));
        }

        private void InitRows()
        {
            if (Columns.Count == 0)
                return;
            // all column always have same field count
            var amount = Columns[0].Fields.Count;
            for (int i = 0; i < amount; ++i)
            {
                var row = new DbTableRow(false, new List<IDbTableField>());
                
                foreach(var column in Columns)
                    row.Fields.Add(column.Fields[i]);
                
                Rows.Add(row);
            }
        }

        public void AddRow(IDbTableFieldFactory creator)
        {
            var row = new DbTableRow(false, new List<IDbTableField>(Columns.Count));
            
            foreach (var column in Columns)
                row.Fields.Add(creator.CreateField(column.FieldDataSource, column.GetDefaultValue(), column));

            Rows.Add(row);
        }

        public void DeleteRow(int at)
        {
            
        }
    }
}