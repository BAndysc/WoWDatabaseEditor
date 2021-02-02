using System.Collections.Generic;
using System.Globalization;

namespace WDE.Common.Parameters
{
    public class Parameter : GenericBaseParameter<int>
    {
        public override string ToString(int key)
        {
            SelectOption option = null;
            if (Items?.TryGetValue(key, out option) ?? false)
                return option.Name + " (" + key + ")";
            return key.ToString();
        }

        public static Parameter Instance { get; } = new Parameter();
    }
    
    public class FloatIntParameter : Parameter
    {
        private readonly float divider;

        public FloatIntParameter(float divider)
        {
            this.divider = divider;
        }
        
        public override string ToString(int value)
        {
            return (value / divider).ToString(CultureInfo.InvariantCulture);
        }
    }

    public class FloatParameter : GenericBaseParameter<float>
    {
        public static FloatParameter Instance { get; } = new FloatParameter();
        
        public override string ToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
    
    public class StringParameter : GenericBaseParameter<string>
    {
        public override string ToString(string value) => value;
    }

    public class FlagParameter : Parameter
    {
        public override string ToString(int value)
        {
            var flags = new List<string>();
            if (value == 0 && Items.TryGetValue(0, out var zero))
                return zero.Name;
            for (var i = 0; i < 31; ++i)
            {
                if (((1 << i) & value) > 0)
                {
                    if (Items.ContainsKey(1 << i))
                        flags.Add(Items[1 << i].Name);
                    else
                        flags.Add("Flag unknown " + (1 << i));
                }
            }

            return string.Join(", ", flags);
        }
    }
}