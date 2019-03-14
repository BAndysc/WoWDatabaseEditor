using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Parameters
{
    public class ParameterChangedValue<T> : EventArgs
    {
        public readonly T Old;
        public readonly T New;

        public ParameterChangedValue(T old, T nnew)
        {
            Old = old;
            New = nnew;
        }
    }

    public abstract class GenericBaseParameter<T> : INotifyPropertyChanged
    {
        public event EventHandler<ParameterChangedValue<T>> OnValueChanged = delegate { };
        protected T _value;

        public string Name { get; set; }
        public string Description { get; set; }
        public string String => ToString();
        public Dictionary<T, SelectOption> Items { get; set; }

        public T Value
        {
            get { return _value; }
            set
            {
                T old = Value;
                _value = value;
                if (!old.Equals(value))
                {
                    OnValueChanged(this, new ParameterChangedValue<T>(old, Value));
                    OnPropertyChanged("Value");
                    OnPropertyChanged("String");
                }
            }
        }

        protected GenericBaseParameter(string name)
        {
            Name = name;
        }

        public void SetValue(T value)
        {
            Value = value;
        }

        public T GetValue()
        {
            return Value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SelectOption
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public SelectOption(string name, string description)
        {
            Name = name;
            Description = description;
        }
        public SelectOption(string name) : this(name, null) { }
        public SelectOption() { }
    }
}
