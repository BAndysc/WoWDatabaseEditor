namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableField
    {
        string FieldName { get; }
        string DbFieldName { get; }
        bool IsReadOnly { get; }
        bool IsModified { get; set; }
        string ValueType { get; }
        bool IsParameter { get; }
        string SqlStringValue { get; }
    }
}