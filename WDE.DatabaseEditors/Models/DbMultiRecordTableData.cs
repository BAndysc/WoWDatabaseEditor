using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public class DbMultiRecordTableData : IDbTableData
    {
        public string TableName { get; }
        public string DbTableName { get; }
        public string TableIndexFieldName { get; }
        public string TableIndexValue { get; }
        public List<IDbTableColumn> Columns { get; }

        public DbMultiRecordTableData(string tableName, string dbTableName, string tableIndexFieldName, 
            string tableIndexValue, List<IDbTableColumn> columns)
        {
            TableName = tableName;
            DbTableName = dbTableName;
            TableIndexFieldName = tableIndexFieldName;
            TableIndexValue = tableIndexValue;
            Columns = columns;
        }

        public void AddRow(IDbTableFieldFactory creator)
        {
            foreach (var column in Columns)
                column.Fields.Add(creator.CreateField(column.FieldDataSource, column.GetDefaultValue(), column));
        }

        public void DeleteRow(int at)
        {
            foreach (var column in Columns)
            {
                if (at < column.Fields.Count)
                    column.Fields.RemoveAt(at);
            }
        }
    }
}