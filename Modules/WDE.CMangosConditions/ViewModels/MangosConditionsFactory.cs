using System.Collections.Generic;
using WDE.CMangosConditions.Data;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Module.Attributes;

namespace WDE.CMangosConditions.ViewModels
{
    [UniqueProvider]
    internal interface IMangosConditionsFactory
    {
        MangosConditionViewModel Create(int type);
        MangosConditionViewModel Create(IMangosConditionLine line);
        /// <summary>Placeholder for a referenced condition_entry that was not among the passed lines.</summary>
        MangosConditionViewModel CreateMissing(uint entry);
        void Update(int type, MangosConditionViewModel viewModel);
    }

    [AutoRegister]
    internal class MangosConditionsFactory : IMangosConditionsFactory
    {
        private readonly IMangosConditionDataManager dataManager;
        private readonly IParameterFactory parameterFactory;

        public MangosConditionsFactory(IMangosConditionDataManager dataManager, IParameterFactory parameterFactory)
        {
            this.dataManager = dataManager;
            this.parameterFactory = parameterFactory;
        }

        public void Update(int type, MangosConditionViewModel viewModel)
        {
            var data = dataManager.TryGetCondition(type);

            for (int i = 0; i < MangosConditionViewModel.ParametersCount; ++i)
                viewModel.GetParameter(i).IsUsed = false;

            if (data == null)
            {
                // unknown type: keep raw editable values so nothing is lost
                for (int i = 0; i < MangosConditionViewModel.ParametersCount; ++i)
                {
                    viewModel.GetParameter(i).Name = $"Value {i + 1}";
                    viewModel.GetParameter(i).Parameter = Parameter.Instance;
                    viewModel.GetParameter(i).IsUsed = true;
                }
                return;
            }

            if (data.Parameters != null)
            {
                int j = 0;
                foreach (var param in data.Parameters)
                {
                    if (j >= MangosConditionViewModel.ParametersCount)
                        break;
                    var holder = viewModel.GetParameter(j++);
                    holder.IsUsed = true;
                    holder.Name = param.Name;
                    holder.Parameter = MakeParameter(param);
                }
            }

            viewModel.UpdateCondition(data);
        }

        private IParameter<long> MakeParameter(MangosConditionParameterJson param)
        {
            if (param.Values != null && param.Values.Count > 0)
                return new Parameter { Items = new Dictionary<long, SelectOption>(param.Values) };
            return parameterFactory.Factory(string.IsNullOrEmpty(param.Type) ? "Parameter" : param.Type!);
        }

        public MangosConditionViewModel Create(int type)
        {
            var vm = new MangosConditionViewModel();
            Update(type, vm);
            return vm;
        }

        public MangosConditionViewModel Create(IMangosConditionLine line)
        {
            var vm = Create(line.ConditionType);
            vm.OriginalEntry = line.ConditionEntry;
            if (!vm.IsLogical)
            {
                vm.Value1.Value = line.Value1;
                vm.Value2.Value = line.Value2;
                vm.Value3.Value = line.Value3;
                vm.Value4.Value = line.Value4;
            }
            vm.Negate.Value = (line.Flags & 1) != 0 ? 1 : 0;
            vm.SwapTargets.Value = (line.Flags & 2) != 0 ? 1 : 0;
            vm.Comment.Value = line.Comments ?? "";
            return vm;
        }

        public MangosConditionViewModel CreateMissing(uint entry)
        {
            var vm = Create(0);
            vm.OriginalEntry = entry;
            vm.Comment.Value = $"!! missing condition {entry} (referenced but not loaded) !!";
            return vm;
        }
    }
}
