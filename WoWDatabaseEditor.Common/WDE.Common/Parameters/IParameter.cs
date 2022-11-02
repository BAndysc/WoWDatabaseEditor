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
        Func<Task<object?>>? SpecialCommand => null;
    }
    
    public interface IParameter<T> : IParameter where T : notnull
    {
        string ToString(T value);

        string ToString(T value, ToStringOptions options) => ToString(value);

        Dictionary<T, SelectOption>? Items { get; }
    }

    public interface IParameterFromString<T>
    {
        T? FromString(string value);
    }

    public struct ToStringOptions
    {
        public bool WithNumber;
        
        public static ToStringOptions WithoutNumber => new ToStringOptions(){WithNumber = false};
    }
    
    public interface IContextualParameter<T, R> : IParameter<T> where T : notnull
    {
        string ToString(T value, R context);
        System.Type ContextType => typeof(R);
        Dictionary<T, SelectOption>? ItemsForContext(R context) => null;
    }

    public interface ICustomPickerContextualParameter<T> : IParameter<T> where T : notnull
    {
        Task<(T, bool)> PickValue(T value, object context);
    }
    
    public interface ICustomPickerParameter<T> : IParameter<T> where T : notnull
    {
        Task<(T, bool)> PickValue(T value);
    }
    
    public interface IAsyncParameter<T> : IParameter<T> where T : notnull
    {
        Task<string> ToStringAsync(T val, CancellationToken token);
    }

    public interface IAsyncContextualParameter<T, R> : IContextualParameter<T, R> where T : notnull
    {
        Task<string> ToStringAsync(T value, CancellationToken token, R context);
    }
}