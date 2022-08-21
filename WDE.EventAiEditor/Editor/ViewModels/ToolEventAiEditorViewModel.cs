using WDE.Common.Windows;
using WDE.EventAiEditor.Editor.ViewModels.Editing;
using WDE.EventAiEditor.Models;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.EventAiEditor.Editor.ViewModels;

public class ToolEventAiEditorViewModel : ObservableBase, ITool, IToolEventAiEditorViewModel
{
    public bool IsOpened => Visibility;
    public string Title => "Event AI edit";
    public string UniqueId => "event_ai_edit";
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Left;
    public bool OpenOnStart => false;
    private bool isSelected;
    private bool visibility;

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
        
    public bool Visibility
    {
        get => visibility;
        set => SetProperty(ref visibility, value);
    }
        
    public EventAiBase? CurrentScript { get; private set; }

    private ReactiveProperty<ParametersEditViewModel?> editorViewModel = new(null);
    public ParametersEditViewModel? EditorViewModel { get; private set; }

    public ToolEventAiEditorViewModel()
    {
        Link(editorViewModel, () => EditorViewModel);
    }
        
    public void ShowEditor(EventAiBase? script, ParametersEditViewModel? viewModel)
    {
        CurrentScript = script;
        editorViewModel.Value = viewModel;
    }
}