using System.Collections.Generic;
using Prism.Mvvm;
using SmartFormat;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.Parameters;
using WDE.Parameters.Models;

namespace WDE.Conditions.ViewModels
{
    public class ConditionViewModel : BindableBase
    {
        public static int ParametersCount => 3;
        private ParameterValueHolder<long>[] parameters = new ParameterValueHolder<long>[ParametersCount];
        private string? readableHint;
        private int conditionId;

        public int ConditionId => conditionId;

        public event System.Action<ConditionViewModel, int, int>? ConditionChanged;

        public ParameterValueHolder<long> Invert { get; }
        public ParameterValueHolder<long> ConditionValue1 => parameters[0];
        public ParameterValueHolder<long> ConditionValue2 => parameters[1];
        public ParameterValueHolder<long> ConditionValue3 => parameters[2];
        public ParameterValueHolder<long> ConditionTarget { get; }

        public ConditionViewModel(IParameter<long> targets)
        {
            for (int i = 0; i < ParametersCount; ++i)
                parameters[i] = new ConstContextParameterValueHolder<long, ConditionViewModel>(Parameter.Instance, 0, this);
            ConditionTarget = new ParameterValueHolder<long>("Target", targets, 0);
            ConditionTarget.IsUsed = targets.HasItems && targets.Items!.Count > 1;

            Invert = new ParameterValueHolder<long>("Negated condition", BoolParameter.Instance, 0);
            
            Invert.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
            ConditionValue1.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
            ConditionValue2.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
            ConditionValue3.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
            ConditionTarget.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
        }

        public ParameterValueHolder<long> GetParameter(int i) => parameters[i];

        public string Readable
        {
            get {
                string? readable = readableHint;
                if (readable == null)
                    return "";
                return Smart.Format(readable, new
                {
                    target = "[s]" + ConditionTarget + "[/s]",
                    negate = Invert.Value == 0,
                    pram1 = "[p]" + GetParameter(0) + "[/p]",
                    pram2 = "[p]" + GetParameter(1) + "[/p]",
                    pram3 = "[p]" + GetParameter(2) + "[/p]",
                    pram1value = GetParameter(0).Value,
                    pram2value = GetParameter(1).Value,
                    pram3value = GetParameter(2).Value
                });
            }
        }

        internal void UpdateCondition(ConditionJsonData data)
        {
            var old = conditionId;
            conditionId = data.Id;
            readableHint = data.Description;
            RaisePropertyChanged(nameof(Readable));
            ConditionChanged?.Invoke(this, old, conditionId);
        }
        
        public ICondition ToCondition(int elseGroup)
        {
            return new AbstractCondition()
            {
                ConditionType = conditionId,
                ConditionValue1 = (int)ConditionValue1.Value,
                ConditionValue2 = (int)ConditionValue2.Value,
                ConditionValue3 = (int)ConditionValue3.Value,
                ConditionTarget = (byte)ConditionTarget.Value,
                ElseGroup = elseGroup,
                NegativeCondition = Invert.Value == 0 ? 0 : 1,
                Comment = Readable.RemoveTags(),
            };
        }

        public IEnumerable<ParameterValueHolder<long>> Values()
        {
            for (int i = 0; i < ParametersCount; ++i)
                yield return GetParameter(i);
            yield return Invert;
            yield return ConditionTarget;
        }
    }
}