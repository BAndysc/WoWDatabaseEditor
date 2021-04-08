namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableField
    {
        string FieldName { get; }
        string DbFieldName { get; }
        bool IsReadOnly { get; }
        string ValueType { get; }
        bool IsParameter { get; }
        bool IsModified { get; }
        string SqlStringValue();
    }
}