using Prism.Commands;
using WDE.MVVM;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public class EditableParameterViewModel<T> : ObservableBase, IEditableParameterViewModel
    {
        private readonly IItemFromListProvider itemFromListProvider;

        public EditableParameterViewModel(ParameterValueHolder<T> parameter, string group, IItemFromListProvider itemFromListProvider)
        {
            this.itemFromListProvider = itemFromListProvider;
            Group = group;
            Parameter = parameter;
            SelectItemAction = new DelegateCommand(SelectItem);

            Watch(parameter, p => p.IsUsed, nameof(IsHidden));
            Watch(parameter, p => p.Parameter, nameof(HasItems));
            Watch(parameter, p => p.Name, nameof(Name));
        }

        public ParameterValueHolder<T> Parameter { get; set; }

        public string Group { get; }
        
        public DelegateCommand SelectItemAction { get; set; }

        public string Name => Parameter.Name;

        public bool IsHidden => !Parameter.IsUsed;
        
        public bool HasItems => Parameter.Parameter.HasItems;
        
        public bool BoolIsChecked
        {
            get
            {
                if (Parameter is ParameterValueHolder<int> p)
                    return p.Value == 1;
                return false;
            }
            set
            {
                if (Parameter is ParameterValueHolder<int> p)
                    p.Value = value ? 1 : 0;
                RaisePropertyChanged();
            }
        }
        
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