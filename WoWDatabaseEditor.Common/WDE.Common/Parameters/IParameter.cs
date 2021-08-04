using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WDE.Common.Parameters
{
    public interface IParameter
    {
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

    public struct ToStringOptions
    {
        public bool WithNumber;
    }

    public interface IContextualParameter<T, R> : IParameter<T> where T : notnull
    {
        string ToString(T value, R context);
        System.Type ContextType => typeof(R);
        Dictionary<T, SelectOption>? ItemsForContext(R context) => null;
    }
}