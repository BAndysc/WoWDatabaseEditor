using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Utils;
using WDE.DatabaseDefinitionEditor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor;

[AutoRegister]
public class DefinitionsEditorToolMenu : IToolMenuItem
{
    private readonly Func<ToolsViewModel> creator;
    private readonly Lazy<IWindowManager> windowManager;
    public string ItemName => "Table definitions editor";
    public ICommand ItemCommand { get; }
    public MenuShortcut? Shortcut => null;

    private IAbstractWindowView? openedWindow;
    
    public DefinitionsEditorToolMenu(Func<ToolsViewModel> creator,
        Lazy<IWindowManager> windowManager)
    {
        this.creator = creator;
        this.windowManager = windowManager;
        ItemCommand = new DelegateCommand(() =>
        {
            if (openedWindow != null)
                openedWindow.Activate();
            else
                OpenNewWindow().ListenErrors();
        });
    }

    private async Task OpenNewWindow()
    {
        var vm = creator();
        vm.ConfigurableOpened();
        openedWindow = windowManager.Value.ShowWindow(vm, out var task);
        await task;
        openedWindow = null;
    }
}