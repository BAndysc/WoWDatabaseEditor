using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseField
    {
        DbEditorTableGroupFieldJson FieldMetaData { get; }
        string FieldName { get; }
        bool IsParameter { get; }
        bool IsModified { get; }
        string SqlStringValue();
    }
}