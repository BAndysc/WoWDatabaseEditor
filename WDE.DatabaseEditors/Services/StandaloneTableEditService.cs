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
    private readonly IStandaloneTableEditorSettings windowsSettings;
    private Dictionary<(string, DatabaseKey?), IAbstractWindowView> openedWindows = new();

    internal StandaloneTableEditService(IWindowManager windowManager,
        ITableOpenService tableOpenService,
        IContainerProvider containerProvider,
        ITableDefinitionProvider definitionProvider,
        IStandaloneTableEditorSettings windowsSettings)
    {
        this.windowManager = windowManager;
        this.containerProvider = containerProvider;
        this.definitionProvider = definitionProvider;
        this.windowsSettings = windowsSettings;
    }
    
    public void OpenEditor(string table, DatabaseKey? key)
    {
        if (openedWindows.TryGetValue((table, key), out var window))
        {
            window.Activate();
            return;
        }
        
        var definition = definitionProvider.GetDefinition(table);
        if (definition == null)
            throw new UnsupportedTableException(table);

        if (key.HasValue && definition.GroupByKeys.Count != key.Value.Count)
        {
            var expectedKeys = "(" + string.Join(", ", definition.GroupByKeys) + ")";
            throw new UnsupportedTableException($"Trying to edit table {table} with key {key} but it has {expectedKeys} keys");
        }
        
        var solutionItem = key.HasValue
            ? new DatabaseTableSolutionItem(key.Value, true, false, table, definition.IgnoreEquality)
            : new DatabaseTableSolutionItem(table, definition.IgnoreEquality);

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
        if (windowsSettings.GetWindowState(table, out var maximized, out var x, out var y, out var width, out var height))
        {
            window.Reposition(x, y, maximized, width, height);
        }
        openedWindows[(table, key)] = window;
        window.OnClosing += () =>
        {
            var position = window.Position;
            var size = window.Size;
            var maximized = window.IsMaximized;
            windowsSettings.UpdateWindowState(table, maximized, position.x, position.y, size.x, size.y);
        };
        WindowLifetimeTask(table, key, window, task).ListenErrors();
    }

    private async Task WindowLifetimeTask(string table, DatabaseKey? key, IAbstractWindowView window, Task lifetime)
    {
        await lifetime;
        openedWindows.Remove((table, key));
    }
}
