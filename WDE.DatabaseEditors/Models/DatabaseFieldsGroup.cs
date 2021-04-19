using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseFieldsGroup : IDatabaseFieldsGroup
    {
        public string CategoryName { get; }
        public List<IDatabaseField> Fields { get; }

        public DatabaseFieldsGroup(string categoryName, List<IDatabaseField> fields)
        {
            CategoryName = categoryName;
            Fields = fields;
        }
    }
}