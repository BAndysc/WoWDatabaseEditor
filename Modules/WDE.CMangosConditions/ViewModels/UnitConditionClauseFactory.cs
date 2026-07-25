using System.Collections.Generic;
using WDE.CMangosConditions.Data;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Module.Attributes;

namespace WDE.CMangosConditions.ViewModels
{
    [UniqueProvider]
    internal interface IUnitConditionClauseFactory
    {
        UnitConditionClauseViewModel Create(UnitConditionClause clause);
        /// <summary>A fresh clause with a sensible default variable.</summary>
        UnitConditionClauseViewModel CreateDefault();
        void Update(int variableId, UnitConditionClauseViewModel viewModel);
    }

    [AutoRegister]
    internal class UnitConditionClauseFactory : IUnitConditionClauseFactory
    {
        private const int DefaultVariable = 12; // HEALTH_PERCENT

        private readonly IUnitConditionDataManager dataManager;
        private readonly IParameterFactory parameterFactory;

        public UnitConditionClauseFactory(IUnitConditionDataManager dataManager, IParameterFactory parameterFactory)
        {
            this.dataManager = dataManager;
            this.parameterFactory = parameterFactory;
        }

        public void Update(int variableId, UnitConditionClauseViewModel viewModel)
        {
            var data = dataManager.TryGetVariable(variableId);
            if (data == null)
            {
                // unknown variable: keep a raw editable value so nothing is lost
                viewModel.Value.Name = "Value";
                viewModel.Value.Parameter = Parameter.Instance;
            }
            else if (data.Value is { } param)
            {
                viewModel.Value.Name = param.Name;
                viewModel.Value.Parameter = MakeParameter(param);
            }
            else
            {
                viewModel.Value.Name = "Value";
                viewModel.Value.Parameter = Parameter.Instance;
            }
            viewModel.UpdateVariable(variableId, data);
        }

        private IParameter<long> MakeParameter(MangosConditionParameterJson param)
        {
            if (param.Values != null && param.Values.Count > 0)
                return new Parameter { Items = new Dictionary<long, SelectOption>(param.Values) };
            return parameterFactory.Factory(string.IsNullOrEmpty(param.Type) ? "Parameter" : param.Type!);
        }

        public UnitConditionClauseViewModel Create(UnitConditionClause clause)
        {
            var vm = new UnitConditionClauseViewModel(this);
            Update((int)clause.Variable, vm);
            vm.Op.Value = clause.Op;
            vm.Value.Value = clause.Value;
            return vm;
        }

        public UnitConditionClauseViewModel CreateDefault()
        {
            var vm = new UnitConditionClauseViewModel(this);
            Update(DefaultVariable, vm);
            return vm;
        }
    }
}
