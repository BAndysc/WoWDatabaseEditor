using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableFieldsCategory : IDbTableFieldsCategory
    {
        public string CategoryName { get; }
        public List<IDbTableField> Fields { get; }

        public DbTableFieldsCategory(string categoryName, List<IDbTableField> fields)
        {
            CategoryName = categoryName;
            Fields = fields;
        }
    }
}