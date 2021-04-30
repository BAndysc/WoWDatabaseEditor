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
        bool IsNull { get; }
    }

    public interface IParameterValue : INotifyPropertyChanged
    {
        string String { get; }
        string OriginalString { get; }
        void SetNull();
        void Revert();
    }

    public class ParameterValue<T> : IParameterValue
    {
        private readonly ValueHolder<T> value;
        private readonly ValueHolder<T> originalValue;

        public T? Value
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

        public ParameterValue(ValueHolder<T> value, ValueHolder<T> originalValue, IParameter<T> parameter)
        {
            this.value = value;
            this.originalValue = originalValue;
            this.parameter = parameter;
            OriginalString = originalValue.IsNull ? "(null)" : parameter.ToString(originalValue.Value!);
            value.PropertyChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(String));
                OnPropertyChanged(nameof(Value));
            };
        }

        public string String => ToString();
        public string OriginalString { get; }
        
        public void SetNull()
        {
            value.Value = default;
        }

        public void Revert()
        {
            value.Value = originalValue.Value;
        }

        public override string ToString()
        {
            return value.IsNull ? "(null)" : parameter.ToString(Value!);
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
        private T? value;
        public T? Value
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

        public ValueHolder(T? initial)
        {
            value = initial;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<T?, T?>? ValueChanged;

        public override string ToString()
        {
            return IsNull || value == null ? "(null)" : value.ToString()!;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]  string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsNull => value == null;

        public ValueHolder<T> Clone()
        {
            return new ValueHolder<T>(Value);
        }
    }
}