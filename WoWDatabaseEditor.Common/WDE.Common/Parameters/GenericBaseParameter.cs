using System;
using System.Collections.Generic;

namespace WDE.Common.Parameters
{
    public class ParameterChangedValue<T> : EventArgs
    {
        public readonly T New;
        public readonly T Old;

        public ParameterChangedValue(T old, T nnew)
        {
            Old = old;
            New = nnew;
        }
    }

    public abstract class GenericBaseParameter<T> : IParameter<T>
    {
        public Dictionary<T, SelectOption> Items { get; set; }
        public abstract string ToString(T value);
        public virtual bool HasItems => Items != null && Items.Count > 0;
    }

    public class SelectOption
    {
        public SelectOption(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public SelectOption(string name) : this(name, null)
        {
        }

        public SelectOption()
        {
        }

        public string Name { get; set; }
        public string Description { get; set; }
    }
}