using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableColumnCategory : IDbTableColumnCategory
    {
        public string CategoryName { get; }
        public List<IDbTableField> Fields { get; }

        public DbTableColumnCategory(string categoryName, List<IDbTableField> fields)
        {
            CategoryName = categoryName;
            Fields = fields;
        }
    }
}