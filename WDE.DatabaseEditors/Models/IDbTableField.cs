using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableField
    {
        DbEditorTableGroupFieldJson FieldMetaData { get; }
        string FieldName { get; }
        bool IsParameter { get; }
        bool IsModified { get; }
        string SqlStringValue();
    }
}