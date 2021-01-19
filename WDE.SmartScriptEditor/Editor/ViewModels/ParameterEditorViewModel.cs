using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class ParameterEditorViewModel<T> : BindableBase
    {
        private readonly IItemFromListProvider itemFromListProvider;

        public ParameterEditorViewModel(ParameterValueHolder<T> parameter, string group, IItemFromListProvider itemFromListProvider)
        {
            Group = group;
            this.itemFromListProvider = itemFromListProvider;
            Parameter = parameter;
            SelectItemAction = new DelegateCommand(SelectItem);
        }

        public ParameterValueHolder<T> Parameter { get; set; }

        public string Group { get; }

        public DelegateCommand SelectItemAction { get; set; }

        private void SelectItem()
        {
            if (Parameter.Parameter.Items != null)
            {
                if (Parameter is ParameterValueHolder<int> p)
                {
                    int? val = itemFromListProvider.GetItemFromList(p.Parameter.Items, Parameter.Parameter is FlagParameter, p.Value);
                    if (val.HasValue)
                        p.Value = val.Value;   
                }
            }
        }
    }
}