using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels;
using WDE.DatabaseEditors.ViewModels.MultiRow;
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
    
    public async Task<long?> PickByColumn(string table, uint key, string column, long? initialValue)
    {
        var definition = definitionProvider.GetDefinition(table);
        if (definition == null)
            return null;
        
        var solutionItem = await tableOpenService.Create(definition, key);
        if (solutionItem == null)
            return null;

        var tableViewModel = containerProvider.Resolve<MultiRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));
        tableViewModel.AllowMultipleKeys = false;
        if (initialValue.HasValue)
        {
            tableViewModel.ToObservable(that => that.IsLoading)
                .Where(@is => !@is)
                .SubscribeOnce(@is =>
                {
                    var group = tableViewModel.Rows.FirstOrDefault(row => row.Key == key);
                    if (group != null)
                    {
                        var row = group.FirstOrDefault(r =>
                            r.Entity.GetCell(column) is DatabaseField<long> longField &&
                            longField.Current.Value == initialValue);
                        if (row != null)
                        {
                            mainThread.Delay(() => tableViewModel.SelectedRow = row, TimeSpan.FromMilliseconds(1));
                        }
                    }
                });
        }
        var viewModel = containerProvider.Resolve<RowPickerViewModel>((typeof(MultiRowDbTableEditorViewModel), tableViewModel));
        if (await windowManager.ShowDialog(viewModel))
        {
            var col = viewModel.SelectedRow?.Entity.GetCell(column);
            if (col is DatabaseField<long> longColumn)
                return longColumn.Current.Value;
        }
        return null;
    }
}