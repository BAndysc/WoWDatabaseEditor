using System.Collections.Generic;
using System.ComponentModel;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseTableData : IDatabaseTableData
    {
        public string TableName { get; }
        public string DbTableName { get; }
        public string TableIndexFieldName { get; }
        public string TableIndexValue { get; }
        public List<IDatabaseFieldsGroup> Categories { get; }

        public DatabaseTableData(string tableName, string dbTableName, string tableIndexFieldName, string tableIndexValue,
            List<IDatabaseFieldsGroup> categories)
        {
            TableName = tableName;
            DbTableName = dbTableName;
            TableIndexFieldName = tableIndexFieldName;
            TableIndexValue = tableIndexValue;
            Categories = categories;
        }
    }
}