using System.Collections.ObjectModel;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseColumn
    {
        string ColumnName { get; }
        string DbColumnName { get; }
        ObservableCollection<IDatabaseField> Fields { get; }
        DbEditorTableGroupFieldJson FieldDataSource { get; }

        bool CanAddFieldOfType(System.Type fieldType);
        object GetDefaultValue();
    }
}