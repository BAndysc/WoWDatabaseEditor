using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Solution;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableField<T> : IDbTableField, INotifyPropertyChanged, IDbTableHistoryActionSource, IStateRestorableField, ISwappableNameField
    {
        public DbTableField(string fieldName, string inDbFieldName, bool isReadOnly, bool isModified, string valueType, bool isParameter,
            ParameterValueHolder<T> value)
        {
            FieldName = fieldName;
            OriginalName = fieldName;
            DbFieldName = inDbFieldName;
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
            OriginalName = fieldDefinition.Name;
            DbFieldName = fieldDefinition.DbColumnName;
            IsReadOnly = fieldDefinition.IsReadOnly;
            isModified = false;
            ValueType = fieldDefinition.ValueType;
            IsParameter = fieldDefinition.ValueType.EndsWith("Parameter");
            Parameter = value;
            Parameter.OnValueChanged += ParameterOnOnValueChanged;
        }

        public string FieldName { get; private set; }
        public string DbFieldName { get; }
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

        public string ToSqlFieldDescription()
        {
            var paramValue = Parameter.Value is string ? $"\"{Parameter.Value}\"" : $"{Parameter.Value}";
            return $"`{DbFieldName}`={paramValue}";
        }

        // IStateRestorableField
        
        public void RestoreLoadedFieldState(DbTableSolutionItemModifiedField fieldData)
        {
            isModified = true;
            if (fieldData.NewValue != null)
                Parameter.Value = (T) fieldData.NewValue;
        }

        public object? GetValueForPersistence() => Parameter.Value;
        
        // ISwappableNameField
        
        public string OriginalName { get; }

        private IDbTableFieldNameSwapHandler? swapHandler;

        public void RegisterNameSwapHandler(IDbTableFieldNameSwapHandler nameSwapHandler)
        {
            // in reality only long parameters are under swap name handler
            if (Parameter.Value is long longValue)
            {
                swapHandler = nameSwapHandler;
                // initial call to swap names right after data init
                swapHandler?.OnFieldValueChanged(longValue, DbFieldName);
            }
        }
        
        public void UnregisterNameSwapHandler() => swapHandler = null;

        public void UpdateFieldName(string newName)
        {
            FieldName = newName;
            OnPropertyChanged(nameof(FieldName));
        }

        // INotifyPropertyChanged
        
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // IDbTableHistoryActionSource
        
        private void ParameterOnOnValueChanged(ParameterValueHolder<T> param, T oldValue, T newValue)
        {
            historyActionReceiver?.RegisterAction(new DbFieldHistoryAction<T>(this, oldValue, newValue, isModified, true));
            IsModified = true;
            // handler for logic from ISwappableNameField
            if (newValue is long longValue)
                swapHandler?.OnFieldValueChanged(longValue, DbFieldName);
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