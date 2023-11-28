using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.QueryUtils;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class SelectSingleTableViewModel : SelectResultsViewModel
{
    private readonly SqlWorkbenchViewModel vm;

    private SimpleSelect selectSpecs;
    private bool[] isPrimaryKey;
    
    public bool RowsHaveFullPrimaryKey { get; }
    
    public ICommand SetSelectedToNullCommand { get; }
    public ICommand AddRowCommand { get; }
    public ICommand DuplicateRowCommand { get; }
    public ICommand DeleteRowCommand { get; }
    public ICommand CopyInsertCommand { get; }
    public ICommand RefreshCommand { get; }
    public IAsyncCommand ApplyChangesCommand { get; }
    public IAsyncCommand RevertChangesCommand { get; }
    
    public override ImageUri Icon => new("Icons/icon_mini_table_big.png");
    
    public SelectSingleTableViewModel(SqlWorkbenchViewModel vm,
        string title,
        in SelectResult results,
        SimpleSelect selectSpecs, 
        IReadOnlyList<ColumnInfo> columnsInfo) : base(vm, title, results)
    {
        this.vm = vm;
        this.selectSpecs = selectSpecs;
        isPrimaryKey = new bool[Columns.Count];
        var columnsDict = columnsInfo.ToDictionary(c => c.Name);
        for (int i = 0; i < Columns.Count; ++i)
        {
            if (columnsDict.TryGetValue(Columns[i].Header, out var columnInfo))
                isPrimaryKey[i] = columnInfo.IsPrimaryKey;
        }
        
        SetSelectedToNullCommand = new DelegateCommand(() => UpdateSelectedCells(null), () => RowsHaveFullPrimaryKey);
        AddRowCommand = new DelegateCommand(() =>
        {

        }, () => RowsHaveFullPrimaryKey);
        DuplicateRowCommand = new DelegateCommand(() =>
        {

        }, () => RowsHaveFullPrimaryKey);
        DeleteRowCommand = new DelegateCommand(() =>
        {
            foreach (var row in Selection.All())
                deletedRows.Add(row);
            UpdateView();
        }, () => RowsHaveFullPrimaryKey);
        CopyInsertCommand = new DelegateCommand(() =>
        {

        });
        RefreshCommand = new DelegateCommand(() =>
        {
            vm.ExecuteAndOverrideResultsAsync(selectSpecs.ToString(), this).ListenErrors();
        }, () => RowsHaveFullPrimaryKey);
        ApplyChangesCommand = new AsyncAutoCommand(async () =>
        {

        });
        RevertChangesCommand = new AsyncAutoCommand(async () =>
        {
            deletedRows.Clear();
            overrides.Clear();
            UpdateView();
        });

        RowsHaveFullPrimaryKey = columnsInfo.Any(x => x.IsPrimaryKey) &&
                                 columnsInfo.Where(x => x.IsPrimaryKey)
                                     .All(x => Columns.Skip(1).Any(col => col.Header == x.Name));
    }

    public void SortBy(int columnIndex)
    {
        columnIndex--; // because the first column is a special # column
        selectSpecs = selectSpecs.WithOrder($"`{Results.ColumnNames[columnIndex]}` ASC");
        var query = selectSpecs.ToString();
        vm.ExecuteAndOverrideResultsAsync(query, this).ListenErrors();
    }

    public void UpdateResults(in SelectResult results)
    {
        Results = results;
        UpdateView();
        RaisePropertyChanged(nameof(Count));
        RaisePropertyChanged(nameof(Results));
    }

    private bool CanBeEdited()
    {
        if (!RowsHaveFullPrimaryKey)
        {
            vm.MessageBoxService.SimpleDialog("Error", 
                "Can't edit this query", 
                "You can't edit cells in this query, because this SELECT doesn't have a full primary key.").ListenErrors();
            return false;
        }

        return true;
    }
    
    public override void UpdateSelectedCells(string? value)
    {
        if (!CanBeEdited())
            return;
        
        foreach (var row in Selection.All())
        {
            if (row < 0 || row >= Count)
                continue;
            
            overrides[(row, SelectedCellIndex)] = value;
        }
        UpdateView();
    }

    public bool IsColumnPrimaryKey(int columnIndex) => isPrimaryKey[columnIndex];
}