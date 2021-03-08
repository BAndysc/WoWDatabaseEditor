using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using WDE.Common.Annotations;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableField<T> : IDbTableField, INotifyPropertyChanged, IObservableTableField
    {
        // Constructor for serialization purpose
        public DbTableField() { }
        
        public DbTableField(string fieldName, string inDbFieldName, bool isReadOnly, bool isModified, string valueType, bool isParameter,
            T value)
        {
            FieldName = fieldName;
            this.inDbFieldName = inDbFieldName;
            IsReadOnly = isReadOnly;
            this.isModified = isModified;
            ValueType = valueType;
            IsParameter = isParameter;
            fieldValue = value;
        }

        public DbTableField(in DbEditorTableGroupFieldJson fieldDefinition)
        {
            FieldName = fieldDefinition.Name;
            inDbFieldName = fieldDefinition.DbColumnName;
            IsReadOnly = fieldDefinition.IsReadOnly;
            isModified = false;
            ValueType = fieldDefinition.ValueType;
            IsParameter = fieldDefinition.ValueType.EndsWith("Parameter");
            fieldValue = default;
        }
        
        public DbTableField(in DbEditorTableGroupFieldJson fieldDefinition, T value)
        {
            FieldName = fieldDefinition.Name;
            inDbFieldName = fieldDefinition.DbColumnName;
            IsReadOnly = fieldDefinition.IsReadOnly;
            isModified = false;
            ValueType = fieldDefinition.ValueType;
            IsParameter = fieldDefinition.ValueType.EndsWith("Parameter");
            fieldValue = value;
        }

        public string FieldName { get; set; }
        [JsonProperty]
        private string inDbFieldName;
        public bool IsReadOnly { get; set; }
        [JsonProperty]
        private bool isModified;

        [JsonIgnore]
        public bool IsModified
        {
            get => isModified;
            set
            {
                isModified = value;
                OnPropertyChanged(nameof(IsModified));
            }
        }
        public string ValueType { get; set; }
        public bool IsParameter { get; set; }
        [JsonProperty]
        private T fieldValue;

        [JsonIgnore]
        public T Value
        {
            get => fieldValue;
            set
            {
                TableFieldValueChanged.Invoke(this, new TableFieldValueChangedEventArgs(nameof(Value), fieldValue, isModified, value, true));
                fieldValue = value;
                IsModified = true;
                OnPropertyChanged(nameof(Value));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event TableFieldValueChangedEventHandler TableFieldValueChanged = delegate { };

        public void RevertPropertyValueChange(object previousValue, bool previousModified)
        {
            fieldValue = (T)previousValue;
            isModified = previousModified;
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(IsModified));
        }

        public string ToSqlFieldDescription() => $"`{inDbFieldName}`={Value}";
    }
}