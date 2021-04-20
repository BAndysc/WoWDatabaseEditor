using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.Common.History;
using WDE.DatabaseEditors.History;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseField<T> : IDatabaseField, INotifyPropertyChanged
    {
        public DatabaseField(ValueHolder<T> value)
        {
            Parameter = value;
            OriginalValue = new ValueHolder<T>(value.Value);
            
            Parameter.ValueChanged += ParameterOnValueChanged;
        }

        private void ParameterOnValueChanged(T old, T nnew)
        {
            OnChanged?.Invoke(new DatabaseFieldHistoryAction<T>(this, "unk", old, nnew));
            OnPropertyChanged(nameof(IsModified));
        }

        public ValueHolder<T> Parameter { get; }
        public ValueHolder<T> OriginalValue { get; }
        public bool IsModified => Comparer<T>.Default.Compare(Parameter.Value, OriginalValue.Value) != 0;
        public event Action<IHistoryAction>? OnChanged;
        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        public override string? ToString()
        {
            return Parameter.ToString();
        }
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}