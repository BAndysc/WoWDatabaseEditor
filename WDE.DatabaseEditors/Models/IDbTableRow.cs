using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableRow
    {
        // int RowIndex { get; }
        bool IsModified { get; }
        List<IDbTableField> Fields { get; }
    }
}