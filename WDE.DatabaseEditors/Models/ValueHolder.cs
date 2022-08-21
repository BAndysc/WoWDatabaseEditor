using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using WDE.Common.Annotations;
using WDE.Common.Parameters;
using WDE.Common.Utils;

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
        void UpdateFromString(string newValue);
        string? ValueAsString { get; }
        void RaiseChanged();
    }

    public interface IParameterValue<T> : IParameterValue where T : notnull
    {
        object? Context { get; }
        IParameter<T> Parameter { get; }
        T? Value { get; set; }
        Dictionary<T, SelectOption>? Items { get; }
    }

    public sealed class ParameterValue<T, TContext> : IParameterValue<T> where T : notnull
    {
        private readonly TContext context;
        private readonly ValueHolder<T> value;
        private readonly ValueHolder<T> originalValue;

        public object? Context => context;
        
        public T? Value
        {
            get => value.Value;
            set => this.value.Value = value;
        }

        public Dictionary<T, SelectOption>? Items
        {
            get
            {
                if (Parameter is IContextualParameter<T, TContext> contextualParameter)
                    return contextualParameter.ItemsForContext(context);
                return parameter.Items;
            }
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

        public void UpdateFromString(string newValue)
        {
            if (parameter is IParameterFromString<long?> fromString)
            {
                var newVal = fromString.FromString(newValue);
                if (newVal.HasValue)
                    Value = (T)(object)(newVal.Value);
            }
            else
            {
                if (typeof(T) == typeof(string))
                {
                    Value = (T)(object)newValue;
                }
                else if (typeof(T) == typeof(long) && long.TryParse(newValue, out var longValue))
                {
                    Value = (T)(object)(longValue);
                }
                else if (typeof(T) == typeof(double) && double.TryParse(newValue, out var doubleValue))
                {
                    Value = (T)(object)(doubleValue);
                }
                else if (typeof(T) == typeof(float) && float.TryParse(newValue, out var floatValue))
                {
                    Value = (T)(object)(floatValue);
                }   
            }
        }

        public string? ValueAsString => Value?.ToString();
        public void RaiseChanged()
        {
            hasCachedStringValue = false;
            OnPropertyChanged(nameof(String));
            OnPropertyChanged(nameof(Value));
        }

        public event Action<Action<IParameterValue>, Action<IParameterValue>>? ValueChanged;

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
                hasCachedStringValue = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(String));
            }
        }

        public ParameterValue(TContext context, ValueHolder<T> value, ValueHolder<T> originalValue, IParameter<T> parameter)
        {
            this.context = context;
            this.value = value;
            valueForAsyncCalculation = originalValue.Value!;
            this.originalValue = originalValue;
            this.parameter = parameter;
            OriginalString = OriginalToString();
            value.PropertyChanged += (_, _) =>
            {
                hasCachedStringValue = false;
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

        private string OriginalToString()
        {
            if (DefaultIsBlank && Comparer<T>.Default.Compare(originalValue.Value, default) == 0)
                return "";
            if (originalValue.IsNull)
                return "(null)";
            if (parameter is IContextualParameter<T, TContext> contextualParameter)
                return cachedStringValue = contextualParameter.ToString(originalValue.Value!, context);
            return parameter.ToString(originalValue.Value!);
        }
        
        public override string ToString()
        {
            if (DefaultIsBlank && Comparer<T>.Default.Compare(value.Value, default) == 0)
                return "";
            if (value.IsNull)
                return "(null)";
            
            if (hasCachedStringValue)
                return cachedStringValue;
            
            if (parameter is IAsyncContextualParameter<T, TContext> asyncContextualParameter)
            {
                if (!AsyncInProgress)
                    CalculateStringAsync(value.Value!, context, asyncContextualParameter).ListenErrors();
                return parameter.ToString(value.Value!);
            }
            
            hasCachedStringValue = true;
            if (parameter is IContextualParameter<T, TContext> contextualParameter)
                return cachedStringValue = contextualParameter.ToString(value.Value!, context);
            return cachedStringValue = parameter.ToString(value.Value!);
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        
        /// Async
        private CancellationTokenSource? currentToStringCancellationToken;
        private string cachedStringValue = null!;
        private bool hasCachedStringValue;
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
        
        private async System.Threading.Tasks.Task CalculateStringAsync(T v, TContext context, IAsyncContextualParameter<T, TContext> p)
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
                cachedStringValue = ToString();
            }

            OnPropertyChanged(nameof(String));
            currentToStringCancellationToken = null;
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