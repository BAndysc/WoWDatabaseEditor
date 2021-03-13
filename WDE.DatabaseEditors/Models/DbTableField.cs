using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Solution;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableField<T> : IDbTableField, INotifyPropertyChanged, IDbTableHistoryActionSource, IStateRestorableField
    {
        public DbTableField(string fieldName, string inDbFieldName, bool isReadOnly, bool isModified, string valueType, bool isParameter,
            ParameterValueHolder<T> value)
        {
            FieldName = fieldName;
            this.inDbFieldName = inDbFieldName;
            IsReadOnly = isReadOnly;
            this.isModified = isModified;
            ValueType = valueType;
            IsParameter = isParameter;
            Parameter = value;
            Parameter.OnValueChanged += ParameterOnOnValueChanged;
        }

        public DbTableField(in DbEditorTableGroupFieldJson fieldDefinition, ParameterValueHolder<T> value)
        {
            FieldName = fieldDefinition.Name;
            inDbFieldName = fieldDefinition.DbColumnName;
            IsReadOnly = fieldDefinition.IsReadOnly;
            isModified = false;
            ValueType = fieldDefinition.ValueType;
            IsParameter = fieldDefinition.ValueType.EndsWith("Parameter");
            Parameter = value;
            Parameter.OnValueChanged += ParameterOnOnValueChanged;
        }

        public string FieldName { get; }
        private readonly string inDbFieldName;
        public bool IsReadOnly { get; }
        private bool isModified;
        
        public bool IsModified
        {
            get => isModified;
            set
            {
                isModified = value;
                OnPropertyChanged(nameof(IsModified));
            }
        }
        public string ValueType { get; }
        public bool IsParameter { get; }
        
        public ParameterValueHolder<T> Parameter { get; }

        public string ToSqlFieldDescription() => $"`{inDbFieldName}`={Parameter.Value}";

        public void RestoreLoadedFieldState(DbTableSolutionItemModifiedField fieldData)
        {
            isModified = true;
            if (fieldData.NewValue != null)
                Parameter.Value = (T) fieldData.NewValue;
        }

        public object? GetValueForPersistence() => Parameter.Value;
        
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void ParameterOnOnValueChanged(ParameterValueHolder<T> param, T oldValue, T newValue)
        {
            historyActionReceiver?.RegisterAction(new DbFieldHistoryAction<T>(this, oldValue, newValue, isModified, true));
            IsModified = true;
        }

        public void RevertPropertyValueChange(T previousValue, bool previousModified)
        {
            Parameter.Value = previousValue;
            IsModified = previousModified; ;
        }

        private IDbFieldHistoryActionReceiver? historyActionReceiver;
        
        public void RegisterActionReceiver(IDbFieldHistoryActionReceiver receiver) => historyActionReceiver = receiver;

        public void UnregisterActionReceiver() => historyActionReceiver = null;
    }
}