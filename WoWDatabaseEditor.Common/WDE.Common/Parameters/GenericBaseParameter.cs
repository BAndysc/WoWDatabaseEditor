using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WDE.Common.Parameters
{
    public class ParameterChangedValue<T> : EventArgs
    {
        public readonly T New;
        public readonly T Old;

        public ParameterChangedValue(T old, T nnew)
        {
            Old = old;
            New = nnew;
        }
    }

    public abstract class GenericBaseParameter<T> : INotifyPropertyChanged
    {
        protected T value;

        protected GenericBaseParameter(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string String => ToString();
        public Dictionary<T, SelectOption> Items { get; set; }

        public T Value
        {
            get => value;
            set
            {
                T old = Value;
                this.value = value;
                if (old == null || !old.Equals(value))
                {
                    OnValueChanged(this, new ParameterChangedValue<T>(old, Value));
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(String));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<ParameterChangedValue<T>> OnValueChanged = delegate { };

        public void SetValue(T value)
        {
            if (Comparer<T>.Default.Compare(value, Value) != 0)
                Value = value;
        }

        public T GetValue()
        {
            return Value;
        }

        protected virtual void OnPropertyChanged([CallerMemberName]
            string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SelectOption
    {
        public SelectOption(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public SelectOption(string name) : this(name, null)
        {
        }

        public SelectOption()
        {
        }

        public string Name { get; set; }
        public string Description { get; set; }
    }
}