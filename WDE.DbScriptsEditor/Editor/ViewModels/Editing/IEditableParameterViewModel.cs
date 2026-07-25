using System.ComponentModel;

namespace WDE.DbScriptsEditor.Editor.ViewModels.Editing
{
    // Copy-adapted from WDE.EventAiEditor so the dbscript "edit action" dialog renders exactly
    // like the EventAI/SmartScript parameter edit dialog.
    public interface IEditableParameterViewModel : INotifyPropertyChanged
    {
        bool IsHidden { get; }
        string Group { get; }
        string Name { get; }
        bool FocusFirst { get; }
        int Order { get; set; }
    }
}
