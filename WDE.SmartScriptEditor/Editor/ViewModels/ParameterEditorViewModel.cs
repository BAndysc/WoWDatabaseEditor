using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Parameters;
using WDE.Common.Providers;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParameterEditorViewModel<T> : BindableBase
    {
        private readonly IItemFromListProvider itemFromListProvider;

        public ParameterEditorViewModel(GenericBaseParameter<T> parameter, string group, IItemFromListProvider itemFromListProvider)
        {
            Group = group;
            this.itemFromListProvider = itemFromListProvider;
            Parameter = parameter;
            SelectItemAction = new DelegateCommand(SelectItem);
        }

        public GenericBaseParameter<T> Parameter { get; set; }

        public string Group { get; }

        public DelegateCommand SelectItemAction { get; set; }

        private void SelectItem()
        {
            if (Parameter.Items != null)
            {
                if (Parameter is Parameter p)
                {
                    int? val = itemFromListProvider.GetItemFromList(p.Items, Parameter is FlagParameter);
                    if (val.HasValue)
                        p.Value = val.Value;   
                }
            }
        }
    }
}