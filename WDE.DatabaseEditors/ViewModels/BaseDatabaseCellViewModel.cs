using System.Collections.Generic;
using System.Linq;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels
{
    public class BaseDatabaseCellViewModel : ObservableBase
    {
        public BaseDatabaseCellViewModel(DatabaseEntity parentEntity)
        {
            ParentEntity = parentEntity;
        }

        public DatabaseEntity ParentEntity { get; }
        public IParameterValue? ParameterValue { get; init; }
        
        public IList<object>? Items
        {
            get
            {
                if (ParameterValue is IParameterValue<long> p)
                    return p.Items?.Select(pair => (object)new ParameterOption(pair.Key, pair.Value.Name)).ToList();
                if (ParameterValue is IParameterValue<string> s)
                    return s.Items?.Select(pair => (object)new ParameterStringOption(pair.Key, pair.Value.Name)).ToList();
                return null;
            }
        }

        public Dictionary<long, SelectOption>? Flags => ParameterValue is IParameterValue<long> p ? p.Items : null;

        public object? OptionValue
        {
            get
            {
                if (ParameterValue is IParameterValue<long> p)
                {
                    if (p.Items != null && p.Items.TryGetValue(p.Value, out var option))
                        return new ParameterOption(p.Value, option.Name);
                    return new ParameterOption(p.Value, "Unknown");
                }

                if (ParameterValue is IParameterValue<string> s)
                {
                    if (s.Items != null && s.Items.TryGetValue(s.Value ?? "", out var option))
                        return new ParameterStringOption(s.Value!, option.Name);
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    if (ParameterValue is IParameterValue<long> p)
                    {
                        p.Value = ((ParameterOption)value).Value;
                    }
                    if (ParameterValue is IParameterValue<string> s)
                    {
                        s.Value = ((ParameterStringOption)value).Key;
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
                return $"{Value}: {Name}";
            }
        }
        
        public class ParameterStringOption
        {
            public readonly string Key;

            public ParameterStringOption(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public string Value { get; }

            public override string ToString()
            {
                return Value;
            }
        }

        public bool IsLongValue => ParameterValue is IParameterValue<long>;
        
        public long AsLongValue
        {
            get => ((ParameterValue as IParameterValue<long>)?.Value ?? 0);
            set
            {
                if (ParameterValue is IParameterValue<long> longParam)
                {
                    longParam.Value = value;
                }
            }
        }
        
        public string? AsStringValue
        {
            get
            {
                if (ParameterValue is IParameterValue<string> strParam)
                    return strParam.IsNull ? null : strParam.Value;
                return null;
            }
            set
            {
                if (ParameterValue is IParameterValue<string> strParam)
                {
                    if (value is null)
                        strParam.SetNull();
                    else
                        strParam.Value = value;
                }
            }
        }

        public bool? AsBoolValue
        {
            get
            {
                if (ParameterValue is IParameterValue<long> longParam)
                    return longParam.IsNull ? null : longParam.Value > 0;
                return null;
            }
            set
            {
                if (ParameterValue is IParameterValue<long> longParam)
                {
                    if (value.HasValue)
                        longParam.Value = value.Value ? 1 : 0;
                    else
                        longParam.SetNull();
                }
            }
        }

        public bool HasItems => ParameterValue?.BaseParameter.HasItems ?? false;

        public bool UseItemPicker =>
            (ParameterValue is IParameterValue<long> vm) && !vm.BaseParameter.NeverUseComboBoxPicker && vm.Items != null && vm.Items.Count < 300 ||
            (ParameterValue is IParameterValue<string> vm2) && !vm2.BaseParameter.NeverUseComboBoxPicker && vm2.Items != null && vm2.Items.Count < 300;
        public bool UseFlagsPicker => (ParameterValue is IParameterValue<long> vm) && vm.Parameter.HasItems && vm.Parameter is FlagParameter;
    }
}