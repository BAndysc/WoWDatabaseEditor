using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableRow : IDbTableRow
    {
        public bool IsModified { get; }
        public List<IDbTableField> Fields { get; }

        public DbTableRow(bool isModified, List<IDbTableField> fields)
        {
            // RowIndex = rowIndex;
            IsModified = isModified;
            Fields = fields;
        }
    }
}