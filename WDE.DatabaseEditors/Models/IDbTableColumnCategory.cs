using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableColumnCategory
    {
        string CategoryName { get; }
        List<IDbTableField> Fields { get; }
    }
}