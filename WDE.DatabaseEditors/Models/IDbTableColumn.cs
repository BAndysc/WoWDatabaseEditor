using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableColumn
    {
        string ColumnName { get; }
        string DbColumnName { get; }
        ObservableCollection<IDbTableField> Fields { get; }
        DbEditorTableGroupFieldJson FieldDataSource { get; }

        bool CanAddFieldOfType(System.Type fieldType);
        object GetDefaultValue();
    }
}