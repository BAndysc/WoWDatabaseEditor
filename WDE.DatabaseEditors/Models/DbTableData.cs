using System.Collections.Generic;
using System.ComponentModel;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableData : IDbTableData
    {
        public string TableName { get; }
        public List<IDbTableColumnCategory> Categories { get; }

        public DbTableData(string tableName, List<IDbTableColumnCategory> categories)
        {
            TableName = tableName;
            Categories = categories;
        }
    }
}