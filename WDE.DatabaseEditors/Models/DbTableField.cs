using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableField<T> : IDbTableField// where T : new()
    {
        public DbTableField(string fieldName, bool isReadOnly, bool isModified, string valueType, bool isParameter,
            T value)
        {
            FieldName = fieldName;
            IsReadOnly = isReadOnly;
            IsModified = isModified;
            ValueType = valueType;
            IsParameter = isParameter;
            Value = value;
        }

        public DbTableField(in DbEditorTableGroupFieldJson fieldDefinition)
        {
            FieldName = fieldDefinition.Name;
            IsReadOnly = fieldDefinition.IsReadOnly;
            IsModified = false;
            ValueType = fieldDefinition.ValueType;
            IsParameter = fieldDefinition.ValueType.EndsWith("Parameter");
            Value = default;
        }
        
        public DbTableField(in DbEditorTableGroupFieldJson fieldDefinition, T value)
        {
            FieldName = fieldDefinition.Name;
            IsReadOnly = fieldDefinition.IsReadOnly;
            IsModified = false;
            ValueType = fieldDefinition.ValueType;
            IsParameter = fieldDefinition.ValueType.EndsWith("Parameter");
            Value = value;
        }

        public string FieldName { get; }

        public bool IsReadOnly { get; }
        public bool IsModified { get; set; }
        public string ValueType { get; }
        public bool IsParameter { get; }
        public T Value { get; set; }
    }
}