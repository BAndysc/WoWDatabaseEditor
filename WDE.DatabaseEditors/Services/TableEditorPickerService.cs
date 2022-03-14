using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.DatabaseEditors.ViewModels.SingleRow;
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
    private readonly IWindowManager windowManager;

    public TableEditorPickerService(ITableOpenService tableOpenService, 
        ITableDefinitionProvider definitionProvider, 
        IContainerProvider containerProvider,
        IMainThread mainThread,
        IWindowManager windowManager)
    {
        this.tableOpenService = tableOpenService;
        this.definitionProvider = definitionProvider;
        this.containerProvider = containerProvider;
        this.mainThread = mainThread;
        this.windowManager = windowManager;
    }
    
    public async Task<long?> PickByColumn(string table, DatabaseKey? key, string column, long? initialValue, string? backupColumn = null)
    {
        var definition = definitionProvider.GetDefinition(table);
        if (definition == null)
            throw new UnsupportedTableException(table);
        
        if (definition.RecordMode != RecordMode.SingleRow && !key.HasValue)
            throw new Exception("Pick by column is not supported for multi-row tables without key");
        
        var solutionItem = await tableOpenService.Create(definition, key ?? default);
        if (solutionItem == null)
            throw new UnsupportedTableException(table);

        ViewModelBase tableViewModel;
        if (definition.RecordMode == RecordMode.MultiRecord)
        {
            var multiRow = containerProvider.Resolve<MultiRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            tableViewModel = multiRow;
            multiRow.AllowMultipleKeys = false;
            if (initialValue.HasValue)
            {
                tableViewModel.ToObservable(that => that.IsLoading)
                    .Where(@is => !@is)
                    .SubscribeOnce(@is =>
                    {
                        var group = multiRow.Rows.FirstOrDefault(row => row.Key == key);
                        if (group != null)
                        {
                            var row = group.FirstOrDefault(r =>
                                (r.Entity.GetCell(column) is DatabaseField<long> longField &&
                                 longField.Current.Value == initialValue) ||
                                (backupColumn != null && r.Entity.GetCell(backupColumn) is DatabaseField<long> longField2 &&
                                 longField2.Current.Value == initialValue));
                            if (row != null)
                            {
                                mainThread.Delay(() => multiRow.SelectedRow = row, TimeSpan.FromMilliseconds(1));
                            }
                        }
                    });
            }
        }
        else if (definition.RecordMode == RecordMode.SingleRow)
        {
            var singleRow = containerProvider.Resolve<SingleRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            tableViewModel = singleRow;
            if (key != null)
            {
                singleRow.FilterViewModel.FilterText = $"`{definition.GroupByKeys[0]}` = {key.Value[0]}";
            }
            if (initialValue.HasValue)
            {
                singleRow.TryFind(key.HasValue ? key.Value.WithAlso(initialValue.Value) : new DatabaseKey(initialValue.Value)).ListenErrors();
            }
            if (key != null)
            {
                singleRow.FilterViewModel.ApplyFilter.Execute(null);
            }
        }
        else
            throw new Exception("TemplateMode not (yet?) supported");

        var viewModel = containerProvider.Resolve<RowPickerViewModel>((typeof(ViewModelBase), tableViewModel));
        if (await windowManager.ShowDialog(viewModel))
        {
            var col = viewModel.SelectedRow?.GetCell(column);
            if (col is DatabaseField<long> longColumn)
                return longColumn.Current.Value;
            if (backupColumn != null)
            {
                col = viewModel.SelectedRow?.GetCell(backupColumn);
                if (col is DatabaseField<long> longColumn2)
                    return longColumn2.Current.Value;
            }
        }
        return null;
    }

    public async Task ShowTable(string table, string? condition)
    {
        var definition = definitionProvider.GetDefinition(table);
        if (definition == null)
            throw new UnsupportedTableException(table);

        var solutionItem = new DatabaseTableSolutionItem(definition.Id, definition.IgnoreEquality);

        ViewModelBase tableViewModel;
        if (definition.RecordMode == RecordMode.SingleRow)
        {
            var singleRow = containerProvider.Resolve<SingleRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
            tableViewModel = singleRow;
            if (condition != null)
            {
                singleRow.FilterViewModel.FilterText = condition;
                singleRow.FilterViewModel.ApplyFilter.Execute(null);
            }
        }
        else
            throw new Exception("TemplateMode and MultiRow not (yet?) supported");

        var viewModel = containerProvider.Resolve<RowPickerViewModel>((typeof(ViewModelBase), tableViewModel));
        viewModel.DisablePicking = true;
        await windowManager.ShowDialog(viewModel);
    }
}