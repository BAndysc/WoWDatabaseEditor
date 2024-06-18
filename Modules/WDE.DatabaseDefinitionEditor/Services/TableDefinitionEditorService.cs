using System;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.DatabaseDefinitionEditor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.Services;

[AutoRegister(Platforms.NonBrowser)]
public class TableDefinitionEditorService : ITableDefinitionEditorService
{
    private readonly Func<StandaloneDefinitionEditorViewModel> creator;
    private readonly Lazy<IWindowManager> windowManager;

    private IAbstractWindowView? openedWindow;
    private StandaloneDefinitionEditorViewModel? openedViewModel;

    public TableDefinitionEditorService(Func<StandaloneDefinitionEditorViewModel> creator,
        Lazy<IWindowManager> windowManager)
    {
        this.creator = creator;
        this.windowManager = windowManager;
    }

    private async Task OpenNewWindow()
    {
        openedViewModel = creator();
        openedWindow = windowManager.Value.ShowWindow(openedViewModel, out var task);
        await task;
        openedViewModel.Dispose();
        openedViewModel = null;
        openedWindow = null;
    }

    public void EditDefinition(string path)
    {
        Open();
        openedViewModel!.ToolsViewModel.SwitchToDefinitionEditor();
        openedViewModel.ToolsViewModel.DefinitionEditor.SelectedDefinitionSource =
            openedViewModel.ToolsViewModel.DefinitionEditor.DefinitionSources.FirstOrDefault(x =>
                x.SourceName == "Build");
        openedViewModel.ToolsViewModel.DefinitionEditor.SelectedDefinition =
            openedViewModel.ToolsViewModel.DefinitionEditor.Definitions.FirstOrDefault(d =>
                d.Definition.AbsoluteFileName == path);
    }

    public void Open()
    {
        if (openedWindow != null)
            openedWindow.Activate();
        else
            OpenNewWindow().ListenErrors();
    }
}