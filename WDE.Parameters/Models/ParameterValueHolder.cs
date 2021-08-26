using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.Common.Parameters;

namespace WDE.Parameters.Models
{
    public interface IParameterValueHolder
    {
    }

    public class ParameterValueHolder<T> : IParameterValueHolder, INotifyPropertyChanged where T : notnull
    {
        private T value;
        [NotNull]
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
                OnPropertyChanged(nameof(String));
                OnValueChanged?.Invoke(this, old, value);
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
                OnPropertyChanged(nameof(HasItems));
            }
        }

        private bool isUsed;
        public bool IsUsed
        {
            get => isUsed; 
            set
            {
                isUsed = value;
                OnPropertyChanged();
            }
        }

        private bool forceHidden;
        public bool ForceHidden
        {
            get => forceHidden; 
            set
            {
                forceHidden = value;
                OnPropertyChanged();
            }
        }
        
        private string name;
        public string Name
        {
            get => name; 
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public virtual bool HasItems => Parameter.HasItems;
        
        public virtual Dictionary<T, SelectOption>? Items => Parameter.Items;

        public string String => ToString();

        public override string ToString()
        {
            return parameter.ToString(value);
        }
        
        public virtual string ToString<R>(R context)
        {
            if (parameter is IContextualParameter<T, R> contextualParameter)
                return contextualParameter.ToString(value, context);
            return parameter.ToString(value);
        }

        public ParameterValueHolder(IParameter<T> parameter, T value)
        {
            IsUsed = false;
            name = "";
            this.value = value;
            this.parameter = parameter;
        }
        
        public ParameterValueHolder(string name, IParameter<T> parameter, T value)
        {
            this.name = name;
            IsUsed = true;
            this.value = value;
            this.parameter = parameter;
        }

        public void Copy(ParameterValueHolder<T> other)
        {
            Name = other.Name;
            IsUsed = other.IsUsed;
            Value = other.Value;
            Parameter = other.Parameter;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;

        public event System.Action<ParameterValueHolder<T>, T, T>? OnValueChanged;
        
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName]  string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ConstContextParameterValueHolder<T, R> : ParameterValueHolder<T> where T : notnull
    {
        private R? constContext;
        private bool inToString;

        public override Dictionary<T, SelectOption>? Items
        {
            get
            {
                if (constContext != null && Parameter is IContextualParameter<T, R> contextual)
                {
                    return contextual.ItemsForContext(constContext);
                }

                return Parameter.Items;
            }
        }

        public override string ToString()
        {
            if (inToString || constContext == null)
                return base.ToString();
            
            inToString = true;
            var ret = ToString(constContext);
            inToString = false;
            return ret;
        }
        
        public ConstContextParameterValueHolder(IParameter<T> parameter, T value, R context) : base(parameter, value)
        {
            constContext = context;
        }
        
        public ConstContextParameterValueHolder(IParameter<T> parameter, T value) : base(parameter, value)
        {
        }

        public ConstContextParameterValueHolder(string name, IParameter<T> parameter, T value) : base(name, parameter, value)
        {
        }
    }
}