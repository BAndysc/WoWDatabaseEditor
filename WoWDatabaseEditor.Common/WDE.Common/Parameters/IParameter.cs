using System.Collections.Generic;

namespace WDE.Common.Parameters
{
    public interface IParameter
    {
        bool HasItems { get; }
    }
    
    public interface IParameter<T> : IParameter
    {
        string ToString(T value);
        Dictionary<T, SelectOption> Items { get; }
    }
}