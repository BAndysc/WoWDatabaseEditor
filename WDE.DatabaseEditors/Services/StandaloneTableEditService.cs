using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Database;
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
    private Dictionary<(DatabaseTable, DatabaseKey?, string?), IAbstractWindowView> openedWindows = new();

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
    
    public void OpenEditor(DatabaseTable table, DatabaseKey? key, string? customWhere = null)
    {
        if (openedWindows.TryGetValue((table, key, customWhere), out var window))
        {
            window.Activate();
            return;
        }
        
        var definition = definitionProvider.GetDefinitionByTableName(table);
        if (definition == null)
            throw new UnsupportedTableException(table);

        if (key.HasValue 
            &&
            definition.RecordMode != RecordMode.SingleRow /* for single row, we allow partial keys */
            && definition.GroupByKeys.Count != key.Value.Count)
        {
            var expectedKeys = "(" + string.Join(", ", definition.GroupByKeys) + ")";
            throw new UnsupportedTableException(table, $"Trying to edit table {table} with a key {key} but the expected key was: {expectedKeys}");
        }

        var solutionItem = !key.HasValue || definition.RecordMode == RecordMode.SingleRow
            ? new DatabaseTableSolutionItem(table, definition.IgnoreEquality)
            : new DatabaseTableSolutionItem(key.Value, true, false, table, definition.IgnoreEquality);
            
        ViewModelBase tableViewModel;
        if (definition.RecordMode == RecordMode.MultiRecord)
        {
            var multiRow = containerProvider.Resolve<MultiRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            tableViewModel = multiRow;
        }
        else if (definition.RecordMode == RecordMode.SingleRow)
        {
            var singleRow = containerProvider.Resolve<SingleRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            if (key.HasValue || !string.IsNullOrWhiteSpace(customWhere))
            {
                var where = customWhere ?? (key.HasValue ? string.Join("AND", Enumerable.Range(0, key.Value.Count)
                    .Select(x => $"`{definition.PrimaryKey[x]}` = {key.Value[x]}")) : "");
                singleRow.DefaultPartialKey = key;
                singleRow.FilterViewModel.FilterText = where;
                singleRow.FilterViewModel.SelectedColumn = singleRow.FilterViewModel.RawSqlColumn;
                singleRow.FilterViewModel.ApplyFilter.Execute(null);
            }
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
        windowsSettings.SetupWindow(table, window);
        openedWindows[(table, key, customWhere)] = window;
        WindowLifetimeTask(table, key, customWhere, window, task).ListenErrors();
    }

    public void OpenTemplatesEditor(IReadOnlyList<DatabaseKey> keys, DatabaseTable table)
    {
        var definition = definitionProvider.GetDefinitionByTableName(table);
        if (definition == null)
            throw new UnsupportedTableException(table);

        if (definition.RecordMode != RecordMode.Template)
            throw new UnsupportedTableException(table, nameof(OpenTemplatesEditor) + " can be called only for template tables");

        var solutionItem = new DatabaseTableSolutionItem(table, false);
        solutionItem.Entries.AddRange(keys.Select(key => new SolutionItemDatabaseEntity(key, true, false, null)));

        bool openInNoSaveMode = false;
        var template = containerProvider.Resolve<TemplateDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
        var viewModel = containerProvider.Resolve<RowPickerViewModel>((typeof(ViewModelBase), template), (typeof(bool), openInNoSaveMode));
        var window = windowManager.ShowWindow(viewModel, out var task);
        windowsSettings.SetupWindow(table, window);
    }

    public void OpenMultiRecordEditor(IReadOnlyList<DatabaseKey> partialKeys, DatabaseTable table, params DatabaseTable[] fallbackTables)
    {
        var definition = definitionProvider.GetDefinitionByTableName(table);
        if (definition == null)
        {
            foreach (var fallback in fallbackTables)
            {
                definition = definitionProvider.GetDefinitionByTableName(fallback);
                if (definition != null)
                {
                    table = fallback;
                    break;
                }
            }
            if (definition == null)
                throw new UnsupportedTableException(table);
        }

        if (definition.RecordMode != RecordMode.MultiRecord)
            throw new UnsupportedTableException(table, nameof(OpenTemplatesEditor) + " can be called only for multi record tables");

        var solutionItem = new DatabaseTableSolutionItem(table, false);
        solutionItem.Entries.AddRange(partialKeys.Select(key => new SolutionItemDatabaseEntity(key, true, false, null)));

        bool openInNoSaveMode = false;
        var template = containerProvider.Resolve<MultiRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
        var viewModel = containerProvider.Resolve<RowPickerViewModel>((typeof(ViewModelBase), template), (typeof(bool), openInNoSaveMode));
        var window = windowManager.ShowWindow(viewModel, out var task);
        windowsSettings.SetupWindow(table, window);
    }

    private async Task WindowLifetimeTask(DatabaseTable table, DatabaseKey? key, string? customWhere, IAbstractWindowView window, Task lifetime)
    {
        await lifetime;
        openedWindows.Remove((table, key, customWhere));
    }
}
