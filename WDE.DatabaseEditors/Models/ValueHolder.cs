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

    public interface IParameterValue
    {
    }

    public class ParameterValue<T> : IParameterValue, INotifyPropertyChanged
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

        public ParameterValue(ValueHolder<T> value, IParameter<T> parameter)
        {
            this.value = value;
            this.parameter = parameter;
            value.PropertyChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(String));
                OnPropertyChanged(nameof(Value));
            };
        }

        public string String => ToString();

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
            }
        }

        public ValueHolder(T initial)
        {
            value = initial;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]  string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}