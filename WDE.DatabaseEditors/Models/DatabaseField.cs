using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.Common.History;
using WDE.DatabaseEditors.History;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseField<T> : IDatabaseField, INotifyPropertyChanged
    {
        private readonly string columnName;

        public DatabaseField(string columnName, ValueHolder<T> currentValue)
        {
            this.columnName = columnName;
            CurrentValue = currentValue;
            OriginalValue = new ValueHolder<T>(currentValue.Value);
            CurrentValue.ValueChanged += OnValueChanged;
        }

        private void OnValueChanged(T? old, T? nnew)
        {
            OnChanged?.Invoke(new DatabaseFieldHistoryAction<T>(this, columnName, old, nnew));
            OnPropertyChanged(nameof(IsModified));
        }

        public ValueHolder<T> CurrentValue { get; }
        public ValueHolder<T> OriginalValue { get; }
        public string FieldName => columnName;
        public bool IsModified => CurrentValue.IsNull != OriginalValue.IsNull || Comparer<T>.Default.Compare(CurrentValue.Value, OriginalValue.Value) != 0;
        public event Action<IHistoryAction>? OnChanged;
        public string ToQueryString()
        {
            if (CurrentValue.IsNull)
                return "NULL";
            if (typeof(T) == typeof(long))
                return CurrentValue.Value!.ToString()!;
            if (typeof(T) == typeof(float))
                return (CurrentValue.Value as float?)!.Value.ToString(CultureInfo.InvariantCulture)!;
            if (typeof(T) == typeof(string))
            {
                var value = CurrentValue.Value as string;
                return "\"" +  value!.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            }

            throw new Exception("Unexpected value of type " + typeof(T));
        }

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        public override string? ToString()
        {
            return CurrentValue.ToString();
        }
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}