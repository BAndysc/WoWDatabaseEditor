using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.MVVM;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public abstract class EditableParameterViewModel : ObservableBase {}
    public class EditableParameterViewModel<T> : EditableParameterViewModel, IEditableParameterViewModel, IDialog
    {
        private readonly IItemFromListProvider itemFromListProvider;

        public EditableParameterViewModel(ParameterValueHolder<T> parameter, string group, IItemFromListProvider itemFromListProvider)
        {
            this.itemFromListProvider = itemFromListProvider;
            Group = group;
            Parameter = parameter;
            SelectItemAction = new AsyncAutoCommand(SelectItem);

            Watch(parameter, p => p.IsUsed, nameof(IsHidden));
            Watch(parameter, p => p.Parameter, nameof(HasItems));
            Watch(parameter, p => p.Name, nameof(Name));
        }

        public ParameterValueHolder<T> Parameter { get; set; }

        public string Group { get; }
        
        public ICommand SelectItemAction { get; set; }

        public string Name => Parameter.Name;

        public bool IsHidden => !Parameter.IsUsed;
        
        public bool HasItems => Parameter.Parameter.HasItems;
        
        public bool BoolIsChecked
        {
            get
            {
                if (Parameter is ParameterValueHolder<long> p)
                    return p.Value == 1;
                return false;
            }
            set
            {
                if (Parameter is ParameterValueHolder<long> p)
                    p.Value = value ? 1 : 0;
                RaisePropertyChanged();
            }
        }
        
        private async Task SelectItem()
        {
            if (Parameter.Parameter.Items != null)
            {
                if (Parameter is ParameterValueHolder<long> p)
                {
                    long? val = await itemFromListProvider.GetItemFromList(p.Parameter.Items, Parameter.Parameter is FlagParameter, p.Value);
                    if (val.HasValue)
                        p.Value = val.Value;   
                }
            }
        }

        public int DesiredWidth { get; } = 300;
        public int DesiredHeight { get; } = 50;
        public string Title { get; } = "Editing";
        public bool Resizeable { get; } = false;
        public event Action CloseCancel;
        public event Action CloseOk;
    }
}