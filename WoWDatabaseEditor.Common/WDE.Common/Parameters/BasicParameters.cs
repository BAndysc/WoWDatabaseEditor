using System.Collections.Generic;
using System.Globalization;

namespace WDE.Common.Parameters
{
    public class Parameter : GenericBaseParameter<long>
    {
        public override string ToString(long key)
        {
            if (Items != null && Items.TryGetValue(key, out var option))
                return option.Name;// + " (" + key + ")";
            return key.ToString();
        }

        public static Parameter Instance { get; } = new Parameter();
    }
    
    public class ParameterNumbered : GenericBaseParameter<long>
    {
        public override string ToString(long key, ToStringOptions options)
        {
            if (options.WithNumber)
                return ToString(key);

            if (Items != null && Items.TryGetValue(key, out var option))
                return option.Name;
            return key.ToString();
        }
        
        public override string ToString(long key)
        {
            if (Items != null && Items.TryGetValue(key, out var option))
                return option.Name + " (" + key + ")";
            return key.ToString();
        }
    }
    
    public class FloatIntParameter : Parameter
    {
        private readonly float divider;

        public FloatIntParameter(float divider)
        {
            this.divider = divider;
        }
        
        public override string ToString(long value)
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
        public static StringParameter Instance { get; } = new StringParameter();
        
        public override string ToString(string value) => value;
    }

    public class SwitchStringParameter : GenericBaseParameter<string>
    {
        public SwitchStringParameter(Dictionary<string, SelectOption> options)
        {
            Items = options;
        }

        public override string ToString(string value)
        {
            if (Items != null && Items.TryGetValue(value, out var option))
                return option.Name;
            return "unknown (" + value + ")";
        }
    }

    public class MultiSwitchStringParameter : SwitchStringParameter
    {
        public MultiSwitchStringParameter(Dictionary<string, SelectOption> options) : base(options)
        {
        }

        public override string ToString(string value)
        {
            return value;
        }
    }
    
    public class FlagParameter : Parameter
    {
        public override string ToString(long value)
        {
            if (Items == null)
                return value.ToString();
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