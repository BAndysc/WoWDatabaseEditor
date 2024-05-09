using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.DatabaseEditors.ViewModels.OneToOneForeignKey;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.DatabaseEditors.ViewModels.Template;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.Services;

[AutoRegister]
[SingleInstance]
public class TableEditorPickerService : ITableEditorPickerService
{
    private readonly ITableOpenService tableOpenService;
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly IContainerProvider containerProvider;
    private readonly IMainThread mainThread;
    private readonly Lazy<ISessionService> sessionService;
    private readonly Lazy<IDocumentManager> documentManager;
    private readonly IMessageBoxService messageBoxService;
    private readonly IWindowManager windowManager;
    private readonly IDatabaseTableDataProvider tableDataProvider;
    private readonly IStandaloneTableEditorSettings windowsSettings;

    internal TableEditorPickerService(ITableOpenService tableOpenService, 
        ITableDefinitionProvider definitionProvider, 
        IContainerProvider containerProvider,
        IMainThread mainThread,
        Lazy<ISessionService> sessionService,
        Lazy<IDocumentManager> documentManager,
        IMessageBoxService messageBoxService,
        IWindowManager windowManager,
        IDatabaseTableDataProvider tableDataProvider,
        IStandaloneTableEditorSettings windowsSettings)
    {
        this.tableOpenService = tableOpenService;
        this.definitionProvider = definitionProvider;
        this.containerProvider = containerProvider;
        this.mainThread = mainThread;
        this.sessionService = sessionService;
        this.documentManager = documentManager;
        this.messageBoxService = messageBoxService;
        this.windowManager = windowManager;
        this.tableDataProvider = tableDataProvider;
        this.windowsSettings = windowsSettings;
    }
    
    public async Task<long?> PickByColumn(DatabaseTable table, DatabaseKey? key, string column, long? initialValue, string? backupColumn = null, string? customWhere = null)
    {
        var definition = definitionProvider.GetDefinitionByTableName(table);
        if (definition == null)
            throw new UnsupportedTableException(table);

        if (definition.RecordMode != RecordMode.SingleRow && customWhere != null)
            throw new Exception("customWhere only works with SingleRow");
        
        if (definition.RecordMode != RecordMode.SingleRow && !key.HasValue)
            throw new Exception("Pick by column is not supported for multi-row tables without key");
        
        var solutionItem = await tableOpenService.Create(definition, key ?? default);
        if (solutionItem == null)
            throw new UnsupportedTableException(table);

        bool openInNoSaveMode = IsItemAlreadyOpened((DatabaseTableSolutionItem)solutionItem, out _);
        ViewModelBase tableViewModel;
        if (definition.RecordMode == RecordMode.MultiRecord)
        {
            var multiRow = containerProvider.Resolve<MultiRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem),
                (typeof(DocumentMode), DocumentMode.PickRow));
            tableViewModel = multiRow;
            multiRow.AllowMultipleKeys = false;
            if (initialValue.HasValue)
            {
                tableViewModel.ToObservable(that => that.IsLoading)
                    .Where(@is => !@is)
                    .SubscribeOnce(@is =>
                    {
                        var groupIndex = multiRow.Rows.IndexIf(row => row.Key == key);
                        if (groupIndex != -1)
                        {
                            var group = multiRow.Rows[groupIndex];
                            var rowIndex = group.IndexIf(r =>
                                (r.Entity.GetCell(new ColumnFullName(null, column)) is DatabaseField<long> longField &&
                                 longField.Current.Value == initialValue) ||
                                (backupColumn != null && r.Entity.GetCell(new ColumnFullName(null, backupColumn)) is DatabaseField<long> longField2 &&
                                 longField2.Current.Value == initialValue));
                            if (rowIndex != -1)
                            {
                                mainThread.Delay(() =>
                                {
                                    multiRow.FocusedRowIndex = new VerticalCursor(groupIndex, rowIndex);
                                    multiRow.MultiSelection.Clear();
                                    multiRow.MultiSelection.Add(multiRow.FocusedRowIndex);
                                }, TimeSpan.FromMilliseconds(1));
                            }
                        }
                    });
            }
        }
        else if (definition.RecordMode == RecordMode.SingleRow)
        {
            var singleRow = containerProvider.Resolve<SingleRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem),
                (typeof(DocumentMode), DocumentMode.PickRow));
            tableViewModel = singleRow;
            string? where = customWhere;
            if (key != null)
            {
                if (where == null)
                    where = "";
                else
                    where = $"({where}) AND ";
                where += $"`{definition.TableName}`.`{definition.GroupByKeys[0]}` = {key.Value[0]}";
            }

            async Task SetFilterAndFind()
            {
                if (where != null)
                {
                    singleRow.FilterViewModel.FilterText = where;
                    singleRow.FilterViewModel.SelectedColumn = singleRow.FilterViewModel.RawSqlColumn;
                    await singleRow.FilterViewModel.ApplyFilter.ExecuteAsync();
                }
                if (initialValue.HasValue)
                {
                    await singleRow.TryFind(key?.WithAlso(initialValue.Value) ?? new DatabaseKey(initialValue.Value));
                }
            }

            SetFilterAndFind().ListenErrors();
        }
        else
            throw new Exception("TemplateMode not (yet?) supported");

        using  var viewModel = containerProvider.Resolve<RowPickerViewModel>((typeof(ViewModelBase), tableViewModel), (typeof(bool), openInNoSaveMode));
        
        tableViewModel.EntityPicked += pickedEntity =>
        {
            viewModel.Pick(pickedEntity);
        };
        var task = windowManager.ShowDialog(viewModel, out var window);
        if (window != null)
            windowsSettings.SetupWindow(table, window);
        if (await task)
        {
            var col = viewModel.SelectedRow?.GetCell(new ColumnFullName(null, column));
            if (col is DatabaseField<long> longColumn)
                return longColumn.Current.Value;
            else if (backupColumn != null)
            {
                col = viewModel.SelectedRow?.GetCell(new ColumnFullName(null, backupColumn));
                if (col is DatabaseField<long> longColumn2)
                    return longColumn2.Current.Value;
            }
            LOG.LogWarning($"Couldn't find column {column} or {backupColumn} in {tableViewModel.TableDefinition.Name}");
        }
        return null;
    }

    public async Task ShowTable(DatabaseTable table, string? condition, DatabaseKey? defaultPartialKey = null)
    {
        var definition = definitionProvider.GetDefinitionByTableName(table);
        if (definition == null)
            throw new UnsupportedTableException(table);

        ViewModelBase viewModelBase;
        bool openIsNoSaveMode = false;
        if (definition.RecordMode == RecordMode.SingleRow)
        {
            var solutionItem = new DatabaseTableSolutionItem(definition.Id, definition.IgnoreEquality);
            openIsNoSaveMode = await CheckIfItemIsOpened(solutionItem, definition);
            var singleRow = containerProvider.Resolve<SingleRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            singleRow.DefaultPartialKey = defaultPartialKey;
            viewModelBase = singleRow;
            if (condition != null)
            {
                singleRow.FilterViewModel.FilterText = condition;
                singleRow.FilterViewModel.SelectedColumn = singleRow.FilterViewModel.RawSqlColumn;
                singleRow.FilterViewModel.ApplyFilter.Execute(null);
            }
        }
        else if (definition.RecordMode == RecordMode.MultiRecord)
        {
            Debug.Assert(definition.RecordMode == RecordMode.MultiRecord);
            Debug.Assert(condition == null);
            Debug.Assert(defaultPartialKey.HasValue);
            var solutionItem = new DatabaseTableSolutionItem(defaultPartialKey.Value, true, false, definition.Id, false);
            openIsNoSaveMode = await CheckIfItemIsOpened(solutionItem, definition);
            var multiRow = containerProvider.Resolve<MultiRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            multiRow.AllowMultipleKeys = false;
            viewModelBase = multiRow;
        }
        else
        {
            Debug.Assert(definition.RecordMode == RecordMode.Template);
            Debug.Assert(condition == null);
            Debug.Assert(defaultPartialKey.HasValue);
            
            bool existsInDatabase = false;
            var data = await tableDataProvider.Load(definition.Id, null, null,null, new []{defaultPartialKey.Value});
                
            if (data != null && data.Entities.Count == 1)
                existsInDatabase = data.Entities[0].ExistInDatabase;
            
            var solutionItem = new DatabaseTableSolutionItem(defaultPartialKey.Value, existsInDatabase, false, definition.Id, false);
            openIsNoSaveMode = await CheckIfItemIsOpened(solutionItem, definition);
            var template = containerProvider.Resolve<TemplateDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            template.AllowMultipleKeys = false;
            viewModelBase = template;
        }

        using var viewModel = containerProvider.Resolve<RowPickerViewModel>((typeof(ViewModelBase), viewModelBase), (typeof(bool), openIsNoSaveMode));
        viewModel.DisablePicking = true;
        
        var task = windowManager.ShowDialog(viewModel, out var window);
        if (window != null)
            windowsSettings.SetupWindow(table, window);
        await task;
    }

    public async Task ShowForeignKey1To1(DatabaseTable table, DatabaseKey key)
    {
        var definition = definitionProvider.GetDefinitionByTableName(table);
        if (definition == null)
            throw new UnsupportedTableException(table);

        if (definition.RecordMode != RecordMode.SingleRow)
            throw new Exception("TemplateMode and MultiRow not supported, because we require 1 - 1 relation!");

        var fakeSolutionItem = new DatabaseTableSolutionItem(definition.Id, definition.IgnoreEquality);

        var openIsNoSaveMode = await CheckIfItemIsOpened(fakeSolutionItem, definition);

        using var viewModel = containerProvider.Resolve<OneToOneForeignKeyViewModel>(
            (typeof(DatabaseKey), key), 
            (typeof(bool), openIsNoSaveMode),
            (typeof(DatabaseTableDefinitionJson), definition));

        var task = windowManager.ShowDialog(viewModel, out var window);
        if (window != null)
            windowsSettings.SetupWindow(table, window);
        await task;

        if (viewModel.PendingSaveTask != null)
            await viewModel.PendingSaveTask;
    }
    
    private bool IsItemAlreadyOpened(DatabaseTableSolutionItem fakeSolutionItem, out ISolutionItemDocument document)
    {
        document = null!;
        if (sessionService.Value.IsOpened && !sessionService.Value.IsPaused &&
            documentManager.Value.TryFindDocument(fakeSolutionItem) is { } openedDocument)
        {
            document = openedDocument;
            return true;
        }

        return false;
    }

    private async Task<bool> CheckIfItemIsOpened(DatabaseTableSolutionItem fakeSolutionItem,
        DatabaseTableDefinitionJson definition)
    {
        bool openIsNoSaveMode = false;
        if (IsItemAlreadyOpened(fakeSolutionItem, out var openedDocument))
        {
            var result = await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Document is already opened")
                .SetMainInstruction($"{definition.Id} is already opened")
                .SetContent(
                    "This table is already being edited and you have an active session.\n Editing the same table in a new window would cause a session data loss.\n\nTherefore you can either close the current document or open the table without save feature enabled (you can still generate sql).")
                .WithButton("Close document", true)
                .WithButton("Open table without save", false)
                .Build());
            if (result)
            {
                await openedDocument.CloseCommand!.ExecuteAsync();
                openIsNoSaveMode = documentManager.Value.OpenedDocuments.Contains(openedDocument);
                if (openIsNoSaveMode)
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<Unit>()
                        .SetTitle("Document is still opened")
                        .SetMainInstruction("Document is still opened")
                        .SetContent("You didn't close the document. Opening the table without the save feature.")
                        .Build());
                }
            }
            else
            {
                openIsNoSaveMode = true;
            }
        }

        return openIsNoSaveMode;
    }
}