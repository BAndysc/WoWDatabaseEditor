using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Solution;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableField<T> : IDbTableField, INotifyPropertyChanged, IDbTableHistoryActionSource, IStateRestorableField, ISwappableNameField
    {
        public DbTableField(string fieldName, string inDbFieldName, bool isReadOnly, string valueType, bool isParameter,
            ParameterValueHolder<T> value)
        {
            FieldName = fieldName;
            OriginalName = fieldName;
            DbFieldName = inDbFieldName;
            IsReadOnly = isReadOnly;
            ValueType = valueType;
            IsParameter = isParameter;
            Parameter = value;
            Parameter.OnValueChanged += ParameterOnValueChanged;
            OriginalValue = new ParameterValueHolder<T>(Parameter.Parameter);
            OriginalValue.Copy(Parameter);
        }

        public DbTableField(in DbEditorTableGroupFieldJson fieldDefinition, ParameterValueHolder<T> value)
        {
            FieldName = fieldDefinition.Name;
            OriginalName = fieldDefinition.Name;
            DbFieldName = fieldDefinition.DbColumnName;
            IsReadOnly = fieldDefinition.IsReadOnly;
            ValueType = fieldDefinition.ValueType;
            IsParameter = fieldDefinition.ValueType.EndsWith("Parameter");
            Parameter = value;
            Parameter.OnValueChanged += ParameterOnValueChanged;
            OriginalValue = new ParameterValueHolder<T>(Parameter.Parameter);
            OriginalValue.Copy(Parameter);
        }

        public string FieldName { get; private set; }
        public string DbFieldName { get; }
        public bool IsReadOnly { get; }

        public bool IsModified => !EqualityComparer<T>.Default.Equals(Parameter.Value, OriginalValue.Value);
        public string ValueType { get; }
        public bool IsParameter { get; }
        
        public ParameterValueHolder<T> Parameter { get; }
        public ParameterValueHolder<T> OriginalValue { get; private set; }
        public string OriginalValueTooltip => $"Original value: {OriginalValue.String}";

        private static NumberFormatInfo sqlNumberFormatInfo = new() {NumberDecimalSeparator = "."};
        
        public string SqlStringValue()
        {
            if (typeof(T) == typeof(string))
                return $"\"{Parameter.Value.ToString().Replace("\"", "\\\"")}\"";
            if (Parameter.Value is float fVal)
            {
                return fVal.ToString(sqlNumberFormatInfo);
            }
            if (Parameter.Value is double dVal)
            {
                return dVal.ToString(sqlNumberFormatInfo);
            }

            return $"{Parameter.Value}";
        }

        // IStateRestorableField
        
        public void RestoreLoadedFieldState(DbTableSolutionItemModifiedField fieldData)
        {
            if (fieldData.NewValue != null)
                Parameter.Value = (T) fieldData.NewValue;
            OriginalValue.Value = (T) fieldData.OriginalValue;
            OnPropertyChanged(nameof(IsModified));
        }

        public object? GetValueForPersistence() => Parameter.Value;

        public object GetOriginalValueForPersistence() => OriginalValue.Value;
        
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
        
        private void ParameterOnValueChanged(ParameterValueHolder<T> param, T oldValue, T newValue)
        {
            historyActionReceiver?.RegisterAction(new DbFieldHistoryAction<T>(this, oldValue, newValue));
            OnPropertyChanged(nameof(IsModified));
            // handler for logic from ISwappableNameField
            if (newValue is long longValue)
                swapHandler?.OnFieldValueChanged(longValue, DbFieldName);
        }

        private IDbFieldHistoryActionReceiver? historyActionReceiver;
        
        public void RegisterActionReceiver(IDbFieldHistoryActionReceiver receiver) => historyActionReceiver = receiver;

        public void UnregisterActionReceiver() => historyActionReceiver = null;

    }
}