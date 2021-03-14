using System.Collections.Generic;

namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableFieldsCategory
    {
        string CategoryName { get; }
        List<IDbTableField> Fields { get; }
    }
}