using System.Collections.Generic;
using Prism.Mvvm;
using SmartFormat;
using WDE.CMangosConditions.Data;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;

namespace WDE.CMangosConditions.ViewModels
{
    public class UnitConditionClauseViewModel : BindableBase
    {
        internal static readonly Parameter OpParameter = new()
        {
            Items = new Dictionary<long, SelectOption>
            {
                [0] = new("none (always true)"),
                [1] = new("="),
                [2] = new("≠"),
                [3] = new("<"),
                [4] = new("≤"),
                [5] = new(">"),
                [6] = new("≥"),
            }
        };

        private static readonly string[] OpSymbols = { "", "=", "≠", "<", "≤", ">", "≥" };

        private readonly IUnitConditionClauseFactory factory;
        private int variableId;
        private UnitConditionVariableJson? variableData;

        public event System.Action<UnitConditionClauseViewModel, int, int>? VariableChanged;

        internal UnitConditionClauseViewModel(IUnitConditionClauseFactory factory)
        {
            this.factory = factory;
            Op = new ParameterValueHolder<long>("Operation", OpParameter, 1);
            Value = new ParameterValueHolder<long>("Value", Parameter.Instance, 0);
            Op.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
            Value.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
        }

        public int VariableId => variableId;

        public UnitConditionVariableJson? SelectedVariable
        {
            get => variableData;
            set
            {
                if (value == null || value.Id == variableId)
                    return;
                factory.Update(value.Id, this);
            }
        }

        public ParameterValueHolder<long> Op { get; }
        public ParameterValueHolder<long> Value { get; }

        public string Readable
        {
            get
            {
                var name = variableData?.NameReadable ?? $"(unknown variable {variableId})";
                if (variableId == 0)
                    return "(unused clause)";
                if (Op.Value == 0)
                    return $"(always) {name}";
                var op = Op.Value >= 0 && Op.Value < OpSymbols.Length ? OpSymbols[Op.Value] : $"op {Op.Value}";
                var template = variableData?.Description ?? "{name} {op} {value}";
                return Smart.Format(template, new
                {
                    name,
                    op,
                    value = Value.ToString(),
                    rawvalue = Value.Value,
                });
            }
        }

        internal void UpdateVariable(int id, UnitConditionVariableJson? data)
        {
            var old = variableId;
            variableId = id;
            variableData = data;
            RaisePropertyChanged(nameof(SelectedVariable));
            RaisePropertyChanged(nameof(Readable));
            if (old != id)
                VariableChanged?.Invoke(this, old, id);
        }

        public UnitConditionClause ToClause()
        {
            return new UnitConditionClause
            {
                Variable = (uint)variableId,
                Op = (uint)Op.Value,
                Value = (int)Value.Value,
            };
        }

        public IEnumerable<ParameterValueHolder<long>> Values()
        {
            yield return Op;
            yield return Value;
        }
    }
}
