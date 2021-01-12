using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Parameters;
using WDE.Common.Providers;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParameterEditorViewModel : BindableBase
    {
        private readonly IItemFromListProvider itemFromListProvider;

        public ParameterEditorViewModel(Parameter parameter, string group, IItemFromListProvider itemFromListProvider)
        {
            Group = group;
            this.itemFromListProvider = itemFromListProvider;
            Parameter = parameter;
            SelectItemAction = new DelegateCommand(SelectItem);
        }

        public Parameter Parameter { get; set; }

        public string Group { get; }

        public DelegateCommand SelectItemAction { get; set; }

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