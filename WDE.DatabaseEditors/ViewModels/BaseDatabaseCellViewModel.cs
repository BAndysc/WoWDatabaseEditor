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
                if (ParameterValue is not IParameterValue<long> p)
                    return null;
                return p.Items?.Select(pair => (object)new ParameterOption(pair.Key, pair.Value.Name)).ToList();
            }
        }

        public Dictionary<long, SelectOption>? Flags => ParameterValue is IParameterValue<long> p ? p.Items : null;

        public ParameterOption? OptionValue
        {
            get
            {
                if (ParameterValue is IParameterValue<long> p)
                {
                    if (p.Items != null && p.Items.TryGetValue(p.Value, out var option))
                        return new ParameterOption(p.Value, option.Name);
                    return new ParameterOption(p.Value, "Unknown");
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    if (ParameterValue is IParameterValue<long> p)
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
            get => ((ParameterValue as IParameterValue<long>)?.Value ?? 0);
            set
            {
                if (ParameterValue is IParameterValue<long> longParam)
                {
                    longParam.Value = value;
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
        public bool UseItemPicker => (ParameterValue is IParameterValue<long> vm) && vm.Items != null && vm.Items.Count < 300;
        public bool UseFlagsPicker => (ParameterValue is IParameterValue<long> vm) && vm.Parameter.HasItems && vm.Parameter is FlagParameter;
    }
}