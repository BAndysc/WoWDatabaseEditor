using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.DatabaseDefinitionEditor.ViewModels;

public class StandaloneDefinitionEditorViewModel : ObservableBase, IWindowViewModel, IClosableDialog
{
    public ToolsViewModel ToolsViewModel { get; }

    public StandaloneDefinitionEditorViewModel(ToolsViewModel toolsViewModel)
    {
        ToolsViewModel = toolsViewModel;
        ToolsViewModel.ConfigurableOpened();
        CloseWindow = new DelegateCommand(OnClose);
    }
    
    public ICommand CloseWindow { get; }
    
    public int DesiredWidth => 1280;
    public int DesiredHeight => 1000;
    public string Title => "Table definitions editor";
    public bool Resizeable => true;
    public ImageUri? Icon => new ImageUri("Icons/icon_edit.png");
    
    public void OnClose()
    {
        if (!ToolsViewModel.IsModified)
            Close?.Invoke();
        else
            AskToSaveAndClose().ListenErrors();
    }

    private async Task AskToSaveAndClose()
    {
        if (await ToolsViewModel.DefinitionEditor.EnsureSaved())
            Close?.Invoke();
    }

    public event Action? Close;
}