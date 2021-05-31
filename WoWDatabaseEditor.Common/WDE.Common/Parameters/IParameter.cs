using System.Collections.Generic;

namespace WDE.Common.Parameters
{
    public interface IParameter
    {
        bool HasItems { get; }
    }
    
    public interface IParameter<T> : IParameter where T : notnull
    {
        string ToString(T value);
        Dictionary<T, SelectOption>? Items { get; }
    }

    public interface IContextualParameter<T, R> : IParameter<T> where T : notnull
    {
        string ToString(T value, R context);
        System.Type ContextType => typeof(R);
    }
}