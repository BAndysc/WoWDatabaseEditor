using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseField<T> : IDatabaseField, INotifyPropertyChanged
    {
        public DatabaseField(ValueHolder<T> value)
        {
            Parameter = value;
            OriginalValue = new ValueHolder<T>(value.Value);
            
            Parameter.PropertyChanged += ParameterOnPropertyChanged;
        }

        private void ParameterOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsModified));
        }

        public ValueHolder<T> Parameter { get; }
        public ValueHolder<T> OriginalValue { get; }
        public bool IsModified => Comparer<T>.Default.Compare(Parameter.Value, OriginalValue.Value) != 0;        
        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}