using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.Common.Parameters;

namespace WDE.DatabaseEditors.Models
{
    public interface IValueHolder
    {
    }

    public interface IParameterValue : INotifyPropertyChanged
    {
        string String { get; }
        string OriginalString { get; }
    }

    public class ParameterValue<T> : IParameterValue
    {
        private readonly ValueHolder<T> value;
        public T Value
        {
            get => value.Value;
            set
            {
                if (Comparer<T>.Default.Compare(value, Value) == 0)
                    return;
                
                this.value.Value = value;
            }
        }
        
        private IParameter<T> parameter;
        public IParameter<T> Parameter
        {
            get => parameter;
            set
            {
                if (parameter == value)
                    return;
                
                parameter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(String));
            }
        }

        public ParameterValue(ValueHolder<T> value, T originalValue, IParameter<T> parameter)
        {
            this.value = value;
            this.parameter = parameter;
            OriginalString = parameter.ToString(originalValue);
            value.PropertyChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(String));
                OnPropertyChanged(nameof(Value));
            };
        }

        public string String => ToString();
        public string OriginalString { get; }

        public override string ToString()
        {
            return parameter.ToString(Value);
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ValueHolder<T> : INotifyPropertyChanged, IValueHolder
    {
        private T value;
        public T Value
        {
            get => value;
            set
            {
                if (Comparer<T>.Default.Compare(value, Value) == 0)
                    return;
                
                var old = this.value;
                this.value = value;
                OnPropertyChanged();
                ValueChanged?.Invoke(old, value);
            }
        }

        public ValueHolder(T initial)
        {
            value = initial;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<T, T>? ValueChanged;

        public override string ToString()
        {
            return value?.ToString() ?? "(null)";
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]  string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}