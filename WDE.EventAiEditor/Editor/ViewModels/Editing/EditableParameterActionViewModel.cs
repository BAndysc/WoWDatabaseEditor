using System.Windows.Input;
using Prism.Commands;
using WDE.MVVM;

namespace WDE.EventAiEditor.Editor.ViewModels.Editing
{
    public class EditableParameterActionViewModel : ObservableBase, IEditableParameterViewModel
    {
        public EditableParameterActionViewModel(EditableActionData data)
        {
            Group = data.Group;
            Name = data.Name;
            Command = new DelegateCommand(data.Command);
            Link(data.ButtonName, () => ActionName);
            
            if (data.IsHidden == null)
                IsHidden = false;
            else
                Link(data.IsHidden, () => IsHidden);
        }

        public ICommand Command { get; }

        public string Name { get; }

        public string ActionName { get; private set; } = "";
        
        public string Group { get; }
        
        public bool FocusFirst { get; set; }

        public bool IsHidden { get; private set; }
    }
}