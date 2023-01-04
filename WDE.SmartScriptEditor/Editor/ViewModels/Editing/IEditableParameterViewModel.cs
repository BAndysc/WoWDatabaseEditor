using System.ComponentModel;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public interface IEditableParameterViewModel : INotifyPropertyChanged
    {
        bool IsHidden { get; }
        string Group { get; }
        string Name { get; }
        bool FocusFirst { get; set; }
        bool IsFirstParameter { get; set; }
        bool HoldsMultipleValues { get; }
        IParameter? GenericParameter { get; }
        object? Context { get; }
    }
}