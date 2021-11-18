using System.Collections.Generic;
using System.Linq;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels
{
    public class BaseDatabaseCellViewModel : ObservableBase
    {
        public IParameterValue? ParameterValue { get; init; }
        
        public IList<object>? Items
        {
            get
            {
                if (ParameterValue is not ParameterValue<long> p)
                    return null;
                return p.Parameter.HasItems
                    ? p.Parameter.Items!.Select(pair => (object)new ParameterOption(pair.Key, pair.Value.Name)).ToList()
                    : null;
            }
        }

        public Dictionary<long, SelectOption>? Flags => ParameterValue is ParameterValue<long> p ? p.Parameter.Items : null;

        public ParameterOption? OptionValue
        {
            get
            {
                if (ParameterValue is ParameterValue<long> p)
                {
                    if (p.Parameter.Items != null && p.Parameter.Items.TryGetValue(p.Value, out var option))
                        return new ParameterOption(p.Value, option.Name);
                    return new ParameterOption(p.Value, "Unknown");
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    if (ParameterValue is ParameterValue<long> p)
                    {
                        p.Value = value.Value;
                    }
                }
            }
        }
        
        public class ParameterOption
        {
            public ParameterOption(long value, string name)
            {
                Value = value;
                Name = name;
            }

            public long Value { get; }
            public string Name { get; }

            public override string ToString()
            {
                return $"{Name} ({Value})";
            }
        }

        public long AsLongValue
        {
            get => ((ParameterValue as ParameterValue<long>)?.Value ?? 0);
            set
            {
                if (ParameterValue is ParameterValue<long> longParam)
                {
                    longParam.Value = value;
                }
            }
        }
        
        public bool? AsBoolValue
        {
            get
            {
                if (ParameterValue is ParameterValue<long> longParam)
                    return longParam.IsNull ? null : longParam.Value > 0;
                return null;
            }
            set
            {
                if (ParameterValue is ParameterValue<long> longParam)
                {
                    if (value.HasValue)
                        longParam.Value = value.Value ? 1 : 0;
                    else
                        longParam.SetNull();
                }
            }
        }

        public bool UseItemPicker => (ParameterValue is ParameterValue<long> vm) && vm.Parameter.HasItems && vm.Parameter.Items!.Count < 300;
        public bool UseFlagsPicker => (ParameterValue is ParameterValue<long> vm) && vm.Parameter.HasItems && vm.Parameter is FlagParameter;
    }
}