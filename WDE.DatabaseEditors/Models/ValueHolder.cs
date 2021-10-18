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
        bool IsNull { get; }
        void SetNull();
        void Revert();
        IParameter BaseParameter { get; }
        bool DefaultIsBlank { get; set; }
    }

    public sealed class ParameterValue<T> : IParameterValue where T : notnull
    {
        private readonly ValueHolder<T> value;
        private readonly ValueHolder<T> originalValue;

        public T? Value
        {
            get => value.Value;
            set => this.value.Value = value;
        }

        public IParameter BaseParameter => parameter;

        public bool DefaultIsBlank
        {
            get => defaultIsBlank;
            set
            {
                defaultIsBlank = value;
                OnPropertyChanged(nameof(String));
            }
        }

        private IParameter<T> parameter;
        private bool defaultIsBlank;

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
            OriginalString = ToString(originalValue);
            value.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(String));
                OnPropertyChanged(nameof(Value));
            };
        }

        public string String => ToString();
        public string OriginalString { get; }
        public bool IsNull => value.IsNull;
        
        public void SetNull()
        {
            value.SetNull();
        }

        public void Revert()
        {
            if (originalValue.IsNull)
                value.SetNull();
            else
                value.Value = originalValue.Value;
        }

        private string ToString(ValueHolder<T> val)
        {
            if (DefaultIsBlank && Comparer<T>.Default.Compare(val.Value, default) == 0)
                return "";
            return val.IsNull ? "(null)" : parameter.ToString(val.Value!);
        }
        
        public override string ToString()
        {
            return ToString(value);
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class ValueHolder<T> : INotifyPropertyChanged, IValueHolder
    {
        private bool isNull;
        
        private T? value;
        public T? Value
        {
            get => value;
            set => SetValue(value, value == null);
        }

        public void SetNull()
        {
            SetValue(default, true);
        }

        private void SetValue(T? value, bool isNull)
        {
            if (this.isNull == isNull && Comparer<T>.Default.Compare(value, Value) == 0)
                return;

            var wasNull = this.isNull;
            this.isNull = isNull;
            var old = this.value;
            this.value = value;
            OnPropertyChanged(nameof(Value));
            ValueChanged?.Invoke(old, value, wasNull, isNull);
        }

        public ValueHolder(T? initial, bool isNull)
        {
            value = initial;
            this.isNull = isNull;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<T?, T?, bool, bool>? ValueChanged;

        public override string ToString()
        {
            return IsNull || value == null ? "(null)" : value.ToString()!;
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName]  string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsNull => isNull;

        public ValueHolder<T> Clone()
        {
            return new ValueHolder<T>(Value, isNull);
        }
    }
}