using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

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
    }

    public class NullParameter : Parameter
    {
        public NullParameter() : base("empty")
        {
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

    public class FlagParameter : Parameter
    {
        public FlagParameter(string name) : base(name)
        {
        }

        public override string ToString()
        {
            List<String> flags = new List<string>();
            for (int i = 0; i < 31; ++i)
            {
                if (((1 << i) & GetValue()) > 0)
                {
                    if (Items.ContainsKey(1 << i))
                        flags.Add(Items[1 << i].Name);
                    else
                    {
                        flags.Add("Flag unknown " + (1 << i));
                    }
                }
            }
            return string.Join(", ", flags);
        }
    }

}
