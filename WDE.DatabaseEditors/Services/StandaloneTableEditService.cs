using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.DatabaseEditors.ViewModels.Template;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[AutoRegister]
[SingleInstance]
public class StandaloneTableEditService : IStandaloneTableEditService
{
    private readonly IWindowManager windowManager;
    private readonly IContainerProvider containerProvider;
    private readonly ITableDefinitionProvider definitionProvider;
    private Dictionary<string, IAbstractWindow> openedWindows = new();

    public StandaloneTableEditService(IWindowManager windowManager,
        ITableOpenService tableOpenService,
        IContainerProvider containerProvider,
        ITableDefinitionProvider definitionProvider)
    {
        this.windowManager = windowManager;
        this.containerProvider = containerProvider;
        this.definitionProvider = definitionProvider;
    }
    
    public void OpenEditor(string table)
    {
        if (openedWindows.TryGetValue(table, out var window))
        {
            window.Activate();
            return;
        }
        
        var definition = definitionProvider.GetDefinition(table);
        if (definition == null)
            throw new UnsupportedTableException(table);

        var solutionItem = new DatabaseTableSolutionItem(table, definition.IgnoreEquality);

        ViewModelBase tableViewModel;
        if (definition.RecordMode == RecordMode.MultiRecord)
        {
            var multiRow = containerProvider.Resolve<MultiRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            tableViewModel = multiRow;
        }
        else if (definition.RecordMode == RecordMode.SingleRow)
        {
            var singleRow = containerProvider.Resolve<SingleRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            tableViewModel = singleRow;
        }
        else
        {
            var template = containerProvider.Resolve<TemplateDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            tableViewModel = template;
        }

        bool openInNoSaveMode = false;
        var viewModel = containerProvider.Resolve<RowPickerViewModel>((typeof(ViewModelBase), tableViewModel), (typeof(bool), openInNoSaveMode));
        window = windowManager.ShowWindow(viewModel, out var task);
        openedWindows[table] = window;
        WindowLifetimeTask(table, window, task).ListenErrors();
    }

    private async Task WindowLifetimeTask(string table, IAbstractWindow window, Task lifetime)
    {
        await lifetime;
        openedWindows.Remove(table);
    }
}