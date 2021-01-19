using System.Collections.Generic;

namespace WDE.Common.Parameters
{
    public interface IParameter<T>
    {
        string ToString(T value);
        bool HasItems { get; }
        Dictionary<T, SelectOption> Items { get; }
    }
}