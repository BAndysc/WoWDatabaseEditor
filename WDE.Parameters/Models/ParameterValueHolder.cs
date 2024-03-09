using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using WDE.Common.Annotations;
using WDE.Common.Parameters;
using WDE.Common.Utils;

namespace WDE.Parameters.Models
{
    public interface IParameterValueHolder : INotifyPropertyChanged
    {
        bool HoldsMultipleValues { get; }
        bool IsUsed { get;}
        bool ForceHidden { get; }
        string Name { get; }
        string String { get; }
        IParameter GenericParameter { get; }
    }
    
    public interface IParameterValueHolder<T> : IParameterValueHolder where T : notnull
    {
        IParameter<T> Parameter { get; }
        T Value { get; set; }
    }

    public class ParameterValueHolder<T> : IParameterValueHolder<T>, INotifyPropertyChanged where T : notnull
    {
        private CancellationTokenSource? currentToStringCancellationToken;
        private string cachedStringValue = null!;
        private bool hasCachedStringValue;
        
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
                hasCachedStringValue = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(String));
                OnValueChanged?.Invoke(this, old, value);
            }
        }

        public bool HoldsMultipleValues => false;

        public void RefreshStringText() => hasCachedStringValue = false;

        private IParameter<T> parameter;
        public IParameter<T> Parameter
        {
            get => parameter;
            set
            {
                if (parameter == value)
                    return;

                hasCachedStringValue = false;
                parameter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(String));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(GenericParameter));
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

        public IParameter GenericParameter => Parameter;

        public override string ToString()
        {
            if (hasCachedStringValue)
                return cachedStringValue;
            
            if (parameter is IAsyncParameter<T> asyncParameter)
            {
                if (!AsyncInProgress) 
                    CalculateStringAsync(value, asyncParameter).ListenErrors();
                return parameter.ToString(value);
            }
            
            hasCachedStringValue = true;
            cachedStringValue = parameter.ToString(value);
            return cachedStringValue;
        }
        
        public virtual string ToString<R>(R context)
        {
            if (hasCachedStringValue)
                return cachedStringValue;

            if (parameter is IAsyncContextualParameter<T, R> asyncContextualParameter)
            {
                if (!AsyncInProgress)
                    CalculateStringAsync(value, context, asyncContextualParameter).ListenErrors();
                if (hasCachedStringValue)
                    return cachedStringValue;
                return parameter.ToString(value);
            }

            if (parameter is IContextualParameter<T, R> contextualParameter)
            {
                hasCachedStringValue = true;
                return cachedStringValue = contextualParameter.ToString(value, context);
            }
            
            return ToString();
        }

        public ParameterValueHolder(IParameter<T> parameter, T value)
        {
            IsUsed = false;
            name = "";
            this.value = value;
            this.valueForAsyncCalculation = value;
            this.parameter = parameter;
        }
        
        public ParameterValueHolder(string name, IParameter<T> parameter, T value)
        {
            this.name = name;
            IsUsed = true;
            this.value = value;
            this.valueForAsyncCalculation = value;
            this.parameter = parameter;
        }

        public void Copy(ParameterValueHolder<T> other)
        {
            Name = other.Name;
            IsUsed = other.IsUsed;
            Value = other.Value;
            Parameter = other.Parameter;
        }
        
        public void ForceRefresh()
        {
            hasCachedStringValue = false;
            OnPropertyChanged(nameof(String));
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;

        public event System.Action<ParameterValueHolder<T>, T, T>? OnValueChanged;
        
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName]  string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// Async
        private T valueForAsyncCalculation;
        public bool AsyncInProgress => currentToStringCancellationToken != null && (Comparer<T>.Default.Compare(valueForAsyncCalculation, Value) == 0);
        private async System.Threading.Tasks.Task CalculateStringAsync(T v, IAsyncParameter<T> p)
        {
            currentToStringCancellationToken?.Cancel();
            var token = new CancellationTokenSource();
            valueForAsyncCalculation = v;
            currentToStringCancellationToken = token;
            try
            {
                var result = await p.ToStringAsync(v, currentToStringCancellationToken.Token);
                if (token.IsCancellationRequested)
                    return;

                hasCachedStringValue = true;
                cachedStringValue = result;
            }
            catch (Exception)
            {
                hasCachedStringValue = true;
                cachedStringValue = ToString();
            }

            OnPropertyChanged(nameof(String));
            
            currentToStringCancellationToken = null;
        }
        
        private async System.Threading.Tasks.Task CalculateStringAsync<R>(T v, R context, IAsyncContextualParameter<T, R> p)
        {
            currentToStringCancellationToken?.Cancel();
            var token = new CancellationTokenSource();
            currentToStringCancellationToken = token;
            valueForAsyncCalculation = v;
            try
            {
                var result = await p.ToStringAsync(v, currentToStringCancellationToken.Token, context);
                if (token.IsCancellationRequested)
                    return;

                hasCachedStringValue = true;
                cachedStringValue = result;
            }
            catch (Exception)
            {
                hasCachedStringValue = true;
                cachedStringValue = ToString(context);
            }

            OnPropertyChanged(nameof(String));
            currentToStringCancellationToken = null;
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