using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Generator.Equals;

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
    
    public interface IItemParameter : IParameter<long> {}

    public interface ISpellParameter : IParameter<long> { }

    public abstract class GenericBaseParameter<T> : IParameter<T> where T : notnull
    {
        public Dictionary<T, SelectOption>? Items { get; set; }
        public abstract string ToString(T value);
        public virtual string ToString(T value, ToStringOptions options) => ToString(value);
        public virtual string? Prefix { get; set; }
        public virtual bool HasItems => Items != null && Items.Count > 0;
        public virtual Func<Task<object?>>? SpecialCommand => null;
    }

    [Equatable]
    public partial class SelectOption
    {
        public SelectOption(string name, string? description)
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

        [DefaultEquality]
        public string Name { get; set; } = "";

        [DefaultEquality]
        public string? Description { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}