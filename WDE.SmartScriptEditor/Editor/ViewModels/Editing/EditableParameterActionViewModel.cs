using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.Parameters.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public class EditableParameterActionViewModel : ObservableBase, IEditableParameterViewModel
    {
        public EditableParameterActionViewModel(EditableActionData data)
        {
            Group = data.Group;
            Name = data.Name;
            Command = new AsyncAutoCommand(data.Command);
            Link(data.ButtonName, () => ActionName);
            
            if (data.IsHidden == null)
                IsHidden = false;
            else
                Link(data.IsHidden, () => IsHidden);
        }

        public ICommand Command { get; }

        public string Name { get; }

        public string ActionName { get; protected set; } = "";
        
        public string Group { get; }
        
        public bool FocusFirst { get; set; }
        
        public bool IsFirstParameter { get; set; }

        public bool HoldsMultipleValues => false;

        public IParameter? GenericParameter => null;

        public object? Context => null;

        public bool IsHidden { get; protected set; }
    }

    public partial class NumberedEditableParameterActionViewModel : EditableParameterActionViewModel
    {
        public NumberedEditableParameterActionViewModel(EditableActionData data) : base(data)
        {
            Value = data.Value!;
        }

        public IParameterValueHolder<int> Value { get; }
    }
}