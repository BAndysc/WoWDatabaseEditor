using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public class DbMultiRecordTableData : IDbTableData
    {
        public string TableName { get; }
        public string DbTableName { get; }
        public string TableIndexFieldName { get; }
        public string TableIndexValue { get; }
        public string TableDescription { get; }
        public List<IDbTableColumn> Columns { get; }
        
        public DbMultiRecordTableData(string tableName, string dbTableName, string tableIndexFieldName, 
            string tableIndexValue, string tableDescription, List<IDbTableColumn> columns)
        {
            TableName = tableName;
            DbTableName = dbTableName;
            TableIndexFieldName = tableIndexFieldName;
            TableIndexValue = tableIndexValue;
            TableDescription = tableDescription;
            Columns = columns;
        }
    }
}