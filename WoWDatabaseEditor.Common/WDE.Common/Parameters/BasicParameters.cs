using System.Collections.Generic;
using System.Globalization;

namespace WDE.Common.Parameters
{
    public class Parameter : GenericBaseParameter<int>
    {
        public Parameter(string name) : base(name)
        {
        }

        public override string ToString()
        {
            if (Items != null && Items.ContainsKey(Value))
                return Items[Value].Name;
            return Value.ToString();
        }

        public virtual Parameter Clone()
        {
            return new(Name) {Value = value, Items = Items, Description = Description};
        }
    }

    public class NullParameter : Parameter
    {
        public NullParameter() : base("empty")
        {
        }

        public override Parameter Clone()
        {
            return new NullParameter {Value = value};
        }
    }

    public class FloatIntParameter : Parameter
    {
        public FloatIntParameter(string name) : base(name)
        {
        }

        public override string ToString()
        {
            return (GetValue() / 1000.0f).ToString(CultureInfo.InvariantCulture);
        }

        public override Parameter Clone()
        {
            return new FloatIntParameter(Name) {Value = value};
        }
    }

    public class FloatParameter : GenericBaseParameter<float>
    {
        public FloatParameter(string name) : base(name)
        {
        }

        public override string ToString()
        {
            return GetValue().ToString(CultureInfo.InvariantCulture);
        }
    }
    
    public class StringParameter : GenericBaseParameter<string>
    {
        public StringParameter(string name) : base(name)
        {
        }

        public override string ToString()
        {
            return GetValue();
        }
    }

    public class FlagParameter : Parameter
    {
        public FlagParameter(string name) : base(name)
        {
        }

        public override string ToString()
        {
            var flags = new List<string>();
            for (var i = 0; i < 31; ++i)
            {
                if (((1 << i) & GetValue()) > 0)
                {
                    if (Items.ContainsKey(1 << i))
                        flags.Add(Items[1 << i].Name);
                    else
                        flags.Add("Flag unknown " + (1 << i));
                }
            }

            return string.Join(", ", flags);
        }

        public override Parameter Clone()
        {
            return new FlagParameter(Name) {Value = value};
        }
    }
}