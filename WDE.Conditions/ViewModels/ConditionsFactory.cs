using System.Linq;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Conditions.Data;
using WDE.Module.Attributes;

namespace WDE.Conditions.ViewModels
{
    [UniqueProvider]
    internal interface IConditionsFactory
    {
        ConditionViewModel CreateOr(int conditionSourceType);
        ConditionViewModel? Create(int conditionSourceType, int type);
        ConditionViewModel? Create(int conditionSourceType, ICondition condition);
        void Update(int id, ConditionViewModel viewModel);
    }
    
    [AutoRegister]
    internal class ConditionsFactory : IConditionsFactory
    {
        private readonly IConditionDataManager conditionDataManager;
        private readonly IParameterFactory parameterFactory;

        public ConditionsFactory(IConditionDataManager conditionDataManager, IParameterFactory parameterFactory)
        {
            this.conditionDataManager = conditionDataManager;
            this.parameterFactory = parameterFactory;
        }

        public void Update(int id, ConditionViewModel viewModel)
        {
            if (!conditionDataManager.HasConditionData(id))
                return;
            
            var data = conditionDataManager.GetConditionData(id);

            for (int i = 0; i < ConditionViewModel.ParametersCount; ++i)
                viewModel.GetParameter(i).IsUsed = false;
            
            for (int i = 0; i < ConditionViewModel.StringParametersCount; ++i)
                viewModel.GetStringParameter(i).IsUsed = false;

            if (data.Parameters != null)
            {
                int j = 0;
                foreach (var param in data.Parameters)
                {
                    viewModel.GetParameter(j).IsUsed = true;
                    viewModel.GetParameter(j).Name = param.Name;
                    viewModel.GetParameter(j++).Parameter = parameterFactory.Factory(param.Type);
                }
            }
            if (data.StringParameters != null)
            {
                int j = 0;
                foreach (var param in data.StringParameters)
                {
                    viewModel.GetStringParameter(j).IsUsed = true;
                    viewModel.GetStringParameter(j).Name = param.Name;
                    viewModel.GetStringParameter(j++).Parameter = parameterFactory.FactoryString(param.Type);
                }
            }
            
            viewModel.UpdateCondition(data);
        }

        public ConditionViewModel CreateOr(int conditionSourceType)
        {
            return Create(conditionSourceType, -1)!;
        }

        public ConditionViewModel? Create(int conditionSourceType, int type)
        {
            if (!conditionDataManager.HasConditionSourceData(conditionSourceType))
                return null;
            
            if (!conditionDataManager.HasConditionData(type))
                return null;

            var sourceData = conditionDataManager.GetConditionSourceData(conditionSourceType);
            var targets = new Parameter();
            targets.Items = sourceData.Targets.ToDictionary(x => (long)x.Key,
                x => new SelectOption(x.Value.Description, x.Value.Comment));
            
            var vm = new ConditionViewModel(targets);
            Update(type, vm);
            return vm;
        }
        
        public ConditionViewModel? Create(int conditionSourceType, ICondition condition)
        {
            var vm = Create(conditionSourceType, condition.ConditionType);
            if (vm == null)
                return null;

            vm.Invert.Value = condition.NegativeCondition;
            vm.ConditionValue1.Value = condition.ConditionValue1;
            vm.ConditionValue2.Value = condition.ConditionValue2;
            vm.ConditionValue3.Value = condition.ConditionValue3;
            vm.ConditionStringValue1.Value = condition.ConditionStringValue1;
            vm.ConditionTarget.Value = vm.ConditionTarget.IsUsed ? condition.ConditionTarget : 0;
            return vm;
        }
    }
}