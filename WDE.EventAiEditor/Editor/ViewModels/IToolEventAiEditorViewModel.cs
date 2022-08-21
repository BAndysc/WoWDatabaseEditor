using WDE.EventAiEditor.Editor.ViewModels.Editing;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Editor.ViewModels
{
    public interface IToolEventAiEditorViewModel
    {
        bool IsOpened { get; }
        void ShowEditor(EventAiBase? script, ParametersEditViewModel? viewModel);
        EventAiBase? CurrentScript { get; }
    }
}