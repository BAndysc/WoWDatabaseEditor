using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.Common.Parameters
{
    public interface IParameter
    {
        string? Prefix { get; }
        bool HasItems { get; }
        bool AllowUnknownItems => false;
        bool NeverUseComboBoxPicker => false;
        Func<Task<object?>>? SpecialCommand => null;
    }
    
    public interface IParameter<T> : IParameter where T : notnull
    {
        string ToString(T value);

        string ToString(T value, ToStringOptions options) => ToString(value);

        Dictionary<T, SelectOption>? Items { get; }
    }
    
    public interface IDynamicParameter<T> : IParameter<T> where T : notnull
    {
        event Action<IParameter<T>>? ItemsChanged; 
    }

    public interface IParameterFromString<T>
    {
        T? FromString(string value);
    }

    public interface IContextualParameterFromString<T, TContext>
    {
        T? FromString(string value, TContext context);
    }

    public interface IContextualParameterFromStringAsync<T, TContext>
    {
        Task<T?> FromString(string value, TContext context);
    }

    // a very specific interface for a very specific case.
    // in DatabaseEditors, for custom fields, when trying to edit a field, this can be used to provide custom text in the editor field
    public interface IContextualInterceptValueParameter<T, TContext>
    {
        bool TryInterceptValue(T? value, TContext context, out string? newValue);
    }

    public struct ToStringOptions
    {
        public bool withNumber;
        
        public static ToStringOptions WithoutNumber => new ToStringOptions(){withNumber = false};

        public static ToStringOptions WithNumber => new ToStringOptions(){withNumber = true};
    }

    public interface IContextualParameter<T> : IParameter<T> where T : notnull
    {
        string ToString(T value, object context);
        Dictionary<T, SelectOption>? ItemsForContext(object? context) => null;
    }

    public interface IContextualParameter<T, R> : IContextualParameter<T> where T : notnull
    {
        string ToString(T value, R context);
        System.Type ContextType => typeof(R);
        Dictionary<T, SelectOption>? ItemsForContext(R context) => null;
    }

    public abstract class BaseContextualParameter<T, R> : IContextualParameter<T, R> where T : notnull
    {
        public abstract string? Prefix { get; }
        public abstract bool HasItems { get; }
        public abstract string ToString(T value);
        public abstract Dictionary<T, SelectOption>? Items { get; }

        public string ToString(T value, object context)
        {
            if (context is R r)
                return ToString(value, r);
            return ToString(value);
        }

        public Dictionary<T, SelectOption>? ItemsForContext(object? context)
        {
            if (context is R r)
                return ItemsForContext(r);
            return null;
        }

        public abstract string ToString(T value, R context);
    }

    public interface ICustomPickerContextualParameter<T> : IParameter<T> where T : notnull
    {
        Task<(T, bool)> PickValue(T value, object context);
    }
    
    public interface ICustomPickerParameter<T> : IParameter<T> where T : notnull
    {
        Task<(T, bool)> PickValue(T value);

        async Task<IReadOnlyCollection<T>> PickMultipleValues()
        {
            var (value, ok) = await PickValue(default!);
            if (ok)
                return new[] {value};
            return Array.Empty<T>();
        }
    }
    
    public interface IAsyncParameter<T> : IParameter<T> where T : notnull
    {
        Task<string> ToStringAsync(T val, CancellationToken token);
    }

    public interface IAsyncContextualParameter<T> : IContextualParameter<T> where T : notnull
    {
        Task<string> ToStringAsync(T value, CancellationToken token, object context);
    }

    public interface IAsyncContextualParameter<T, R> : IAsyncContextualParameter<T>, IContextualParameter<T, R> where T : notnull
    {
        Task<string> ToStringAsync(T value, CancellationToken token, R context);
    }

    public abstract class BaseAsyncContextualParameter<T, R> : IAsyncContextualParameter<T, R> where T : notnull
    {
        public async Task<string> ToStringAsync(T value, CancellationToken token, object context)
        {
            if (context is R r)
                return await ToStringAsync(value, token, r);
            return ToString(value);
        }

        public abstract Task<string> ToStringAsync(T value, CancellationToken token, R context);
        public abstract string? Prefix { get; }
        public abstract bool HasItems { get; }
        public abstract string ToString(T value);
        public abstract Dictionary<T, SelectOption>? Items { get; }

        public string ToString(T value, object context)
        {
            if (context is R r)
                return ToString(value, r);
            return ToString(value);
        }

        public abstract string ToString(T value, R context);
    }
}