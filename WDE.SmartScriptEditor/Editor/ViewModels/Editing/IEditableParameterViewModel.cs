using System.ComponentModel;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public interface IEditableParameterViewModel : INotifyPropertyChanged
    {
        bool IsHidden { get; }
        string Group { get; }
        string Name { get; }
    }
}