using System.Collections.Generic;
using System.ComponentModel;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableData : IDbTableData
    {
        public string TableName { get; }
        public string DbTableName { get; }
        public string TableIndexFieldName { get; }
        public string TableIndexValue { get; }
        public string TableDescription { get; }
        public List<IDbTableFieldsCategory> Categories { get; }

        public DbTableData(string tableName, string dbTableName, string tableIndexFieldName, string tableIndexValue,
            string tableDescription, List<IDbTableFieldsCategory> categories)
        {
            TableName = tableName;
            DbTableName = dbTableName;
            TableIndexFieldName = tableIndexFieldName;
            TableIndexValue = tableIndexValue;
            TableDescription = tableDescription;
            Categories = categories;
        }
    }
}