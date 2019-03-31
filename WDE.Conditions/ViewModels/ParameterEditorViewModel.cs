using Prism.Commands;
using Prism.Mvvm;

using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Conditions.Data;

namespace WDE.Conditions.ViewModels
{
    public class ParameterEditorViewModel : BindableBase
    {
        private readonly IItemFromListProvider itemFromListProvider;

        public Parameter Parameter { get; set; }

        public DelegateCommand SelectItemAction { get; set; }

        public ParameterEditorViewModel(ConditionParameterJsonData param, IItemFromListProvider itemFromListProvider)
        {
            this.itemFromListProvider = itemFromListProvider;

            Parameter = new Parameter(param.Name);
            Parameter.Items = param.Values;
            Parameter.Description = param.Description;

            SelectItemAction = new DelegateCommand(SelectItem);
        }

        private void SelectItem()
        {
            if (Parameter.Items != null)
            {
                int? val = itemFromListProvider.GetItemFromList(Parameter.Items, Parameter is FlagParameter);
                if (val.HasValue)
                    Parameter.Value = val.Value;
            }
        }
    }
}
