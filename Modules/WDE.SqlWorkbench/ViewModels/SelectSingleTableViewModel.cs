using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.ActionsOutput;
using WDE.SqlWorkbench.Services.QueryConfirmation;
using WDE.SqlWorkbench.Services.UserQuestions;
using WDE.SqlWorkbench.Utils;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class SelectSingleTableViewModel : SelectResultsViewModel
{
    [Notify] private bool refreshInProgress;

    private readonly SqlWorkbenchViewModel vm;

    private SimpleSelect selectSpecs;
    private List<int> primaryKeyColumnsIndices;
    private List<ColumnInfo> columnsInfo;
    
    public bool RowsHaveFullPrimaryKey { get; }
    
    public DelegateCommand SetSelectedToNullCommand { get; }
    public DelegateCommand AddRowCommand { get; }
    public DelegateCommand DuplicateRowCommand { get; }
    public DelegateCommand DeleteRowCommand { get; }
    public IAsyncCommand RefreshTableCommand { get; }
    public IAsyncCommand ApplyChangesCommand { get; }
    public IAsyncCommand RevertChangesCommand { get; }
    public IAsyncCommand PasteSelectedCommand { get; }

    public override ImageUri Icon => new("Icons/icon_mini_table_big.png");
    
    private int sortByIndex = -1;

    protected override SimpleFrom? FromClause => selectSpecs.From;

    public SelectSingleTableViewModel(SqlWorkbenchViewModel vm,
        IActionOutput action,
        in SelectResult results,
        SimpleSelect selectSpecs, 
        IReadOnlyList<ColumnInfo> tableColumnsInfo) : base(vm, action, results)
    {
        this.vm = vm;
        this.selectSpecs = selectSpecs;
        primaryKeyColumnsIndices = new List<int>();
        columnsInfo = new List<ColumnInfo>();
        UpdateResultsSchema(tableColumnsInfo);
        Title = this.selectSpecs.From.Table;
        
        SetSelectedToNullCommand = new DelegateCommand(() => UpdateSelectedCells(null), () => RowsHaveFullPrimaryKey);
        AddRowCommand = new DelegateCommand(() =>
        {
            AdditionalRowsCount++;
            ColumnsOverride.Each(x => x.TryOverride(Count - 1, null, out _));
            SelectedRowIndex = Count - 1;
            Selection.Clear();
            Selection.Add(SelectedRowIndex);
            for (int i = 0; i < Columns.Count; ++i)
            {
                OverrideValue(SelectedRowIndex, i, columnsInfo[i].DefaultValue?.ToString());
            }
        }, () => RowsHaveFullPrimaryKey && !RefreshInProgress);
        DuplicateRowCommand = new DelegateCommand(() =>
        {
            if (SelectedRowIndex < 0 || SelectedRowIndex >= Count)
                return;
            
            var copyRowIndex = SelectedRowIndex;
            
            AdditionalRowsCount++;
            for (int i = 1; i < Columns.Count; ++i) // skip # column
                OverrideValue(Count - 1, i, GetFullValue(copyRowIndex, i));
            SelectedRowIndex = Count - 1;
            Selection.Clear();
            Selection.Add(SelectedRowIndex);
        }, () => RowsHaveFullPrimaryKey && !RefreshInProgress);
        PasteSelectedCommand = new AsyncAutoCommand(async () =>
        {
            var text = await vm.ClipboardService.GetText();
            if (string.IsNullOrEmpty(text))
                return;
            
            CsvTokenizer tokenizer = new CsvTokenizer(text);
            Selection.Clear();
            while (tokenizer.HasNextLine())
            {
                tokenizer.NextLine();
                int rowIndex = AdditionalRowsCount++ + Results.AffectedRows;
                ColumnsOverride.Each(x => x.TryOverride(Count - 1, null, out _));
                Selection.Add(rowIndex);
                SelectedRowIndex = rowIndex;
                
                int index = 1;
                while (tokenizer.HasNextToken())
                {
                    var token = tokenizer.NextToken();
                    if (index >= Columns.Count)
                        continue;
                    OverrideValue(rowIndex, index++, token);
                }
            }
        }, () => !RefreshInProgress);
        DeleteRowCommand = new DelegateCommand(() =>
        {
            foreach (var row in Selection.All())
                MarkRowAsDeleted(row);
        }, () => RowsHaveFullPrimaryKey && !RefreshInProgress);
        RevertChangesCommand = new AsyncAutoCommand(async () =>
        {
            ResetChanges();
        }, () => !RefreshInProgress);
        RefreshTableCommand = new AsyncAutoCommand(async () =>
        {
            ErrorIfConnectionChanged();
            if (!await AskIfForgetChangesAsync())
                return;
            
            await RevertChangesCommand.ExecuteAsync();
            RefreshInProgress = true;
            try
            {
                await vm.ExecuteAndOverrideResultsAsync(selectSpecs.ToString(), this);
            }
            finally
            {
                RefreshInProgress = false;
            }
        }, () => !RefreshInProgress).WrapMessageBox<Exception>(vm.UserQuestions);

        ApplyChangesCommand = new AsyncAutoCommand(async () =>
        {
            try
            {
                ErrorIfConnectionChanged();
                await ApplyChangesAsync(true);
            }
            catch (TaskCanceledException)
            {
                // ignore on purpose, because it means user canceled the operation
            }
            catch (Exception e)
            {
                LOG.LogError(e, "Error while applying changes");
                await vm.UserQuestions.SaveErrorAsync(e);   
            }
        }, () => !RefreshInProgress);
        
        RowsHaveFullPrimaryKey = columnsInfo.Any(x => x.IsPrimaryKey) &&
                                 columnsInfo.Where(x => x.IsPrimaryKey)
                                     .All(x => Columns.Skip(1).Any(col => col.Header == x.Name));

        On(() => RefreshInProgress, _ =>
        {
            SetSelectedToNullCommand.RaiseCanExecuteChanged();
            AddRowCommand.RaiseCanExecuteChanged();
            DuplicateRowCommand.RaiseCanExecuteChanged();
            DeleteRowCommand.RaiseCanExecuteChanged();
            RefreshTableCommand.RaiseCanExecuteChanged();
            ApplyChangesCommand.RaiseCanExecuteChanged();
            RevertChangesCommand.RaiseCanExecuteChanged();
            PasteSelectedCommand.RaiseCanExecuteChanged();
        });
    }
    
    private void UpdateResultsSchema(IReadOnlyList<ColumnInfo> tableColumnsInfo)
    {
        var columnsDict = tableColumnsInfo.ToDictionary(c => c.Name);
        columnsInfo.Clear();
        primaryKeyColumnsIndices.Clear();
        
        columnsInfo.Add(new ColumnInfo("#", "", false, false, false, null, null, null));
        for (int i = 1; i < Columns.Count; ++i)
        {
            if (columnsDict.TryGetValue(Columns[i].Header, out var columnInfo))
            {
                columnsInfo.Add(columnInfo);
                if (columnInfo.IsPrimaryKey)
                    primaryKeyColumnsIndices.Add(i);
            }
            else
                throw new Exception($"Columns {Columns[i].Header} not found in the table");
        }
    }

    protected override void ResultsUpdated(IReadOnlyList<ColumnInfo>? tableColumnsInfo)
    {
        if (tableColumnsInfo == null)
            throw new Exception("Cannot refresh results, because this query no longer returns a valid table.");

        UpdateResultsSchema(tableColumnsInfo);
    }

    protected override bool IsColumnNullable(int columnIndex) => columnsInfo[columnIndex].IsNullable;

    protected override string? ColumnType(int columnIndex) => columnsInfo[columnIndex].Type;

    private List<string> GenerateChanges()
    {
        string GenerateWhere(int rowIndex)
        {
            return string.Join(" AND ", primaryKeyColumnsIndices.Select(columnIndex =>
            {
                if (columnIndex - 1 >= Results.Columns.Length)
                    return "ERR_COLUMN_NOT_FOUND";
                
                // -1 because the first column is a special # column
                var columnName = Results.ColumnNames[columnIndex - 1];
                var value = Results.Columns[columnIndex - 1]!.GetToString(rowIndex);
                return $"`{columnName}` = {ColumnValueToMySqlRepresentation(columnIndex, value)}";
            }));
        }

        List<string> queries = new();

        List<string> wheres = deletedRows
            .Where(rowIndex => rowIndex < Results.AffectedRows)
            .Select(GenerateWhere)
            .ToList();
        
        if (wheres.Count > 0)
        {
            SimpleDelete delete = new SimpleDelete(selectSpecs.From, string.Join(" OR ", wheres.Select(x => $"({x})")));
            queries.Add(delete.ToString());
        }

        foreach (var alteredRow in alteredRows)
        {
            if (IsRowMarkedAsDeleted(alteredRow))
                continue;
            
            if (alteredRow >= Results.AffectedRows) // inserts
                continue;
            
            var where = GenerateWhere(alteredRow);

            var set = string.Join(", ", ColumnsOverride
                .Select((col, index) => (col, index))
                .Where(pair => pair.col.HasRow(alteredRow))
                .Select(pair =>
            {
                var cellIndex = pair.index;
                var value = pair.col.GetFullToString(alteredRow);

                var columnName = Results.ColumnNames[cellIndex];
                value = ColumnValueToMySqlRepresentation(cellIndex + 1, value);
                return $"`{columnName}` = {value}";
            }));
            
            queries.Add($"UPDATE {selectSpecs.From} SET {set} WHERE {where}");
        }

        if (TryGenerateInsert(Enumerable.Range(Results.AffectedRows, AdditionalRowsCount), true, out var insert))
            queries.Add(insert);

        return queries;
    }

    private async Task<bool> AskIfForgetChangesAsync()
    {
        if (!IsModified)
            return true;
        
        var result = await vm.UserQuestions.AskToRevertChangesAsync();
        return result;
    }
    
    private async Task ApplyChangesAsync(bool refreshOnSuccess)
    {
        var changes = GenerateChanges();
        if (changes.Count == 0)
            return;
        changes.Insert(0, "START TRANSACTION");
        changes.Add("COMMIT");
        var combined = string.Join(";\n", changes);
        var execute = async () =>
        {
            await vm.ExecuteAsync(changes, false, true, true);
            ResetChanges();
            if (refreshOnSuccess)
                await RefreshTableCommand.ExecuteAsync();
        };
        if (vm.Preferences.AskBeforeApplyingChanges)
        {
            if (await vm.QueryConfirmationService.QueryConfirmationAsync(combined, execute) == QueryConfirmationResult.DontExecute)
                throw new TaskCanceledException();
        }
        else
            await execute();
    }
    
    public override async Task<bool> SaveAsync()
    {
        ErrorIfConnectionChanged();

        try
        {
            await ApplyChangesAsync(true);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false; // ignore on purpose, because it means user canceled the operation
        }
        catch (Exception e)
        {
            await vm.UserQuestions.SaveErrorAsync(e);
            return false;
        }
    }

    public void SortBy(int columnIndex)
    {
        async Task SortByAsync()
        {
            if (!await AskIfForgetChangesAsync())
                return;
            
            await RevertChangesCommand.ExecuteAsync();
            
            string order = "ASC";
            if (sortByIndex == columnIndex)
            {
                order = "DESC";
                sortByIndex = -1;
            }
            else
            {
                sortByIndex = columnIndex;    
            }
        
            // -1 because the first column is a special # column
            selectSpecs = selectSpecs.WithOrder($"`{Results.ColumnNames[columnIndex - 1]}` {order}");
            var query = selectSpecs.ToString();
            await vm.ExecuteAndOverrideResultsAsync(query, this);
        }
        SortByAsync().ListenErrors();
    }

    protected override bool CanEditRow(int rowIndex) => rowIndex >= Results.AffectedRows || RowsHaveFullPrimaryKey;

    private bool CanEditSelectedRow()
    {
        if (RowsHaveFullPrimaryKey)
            return true;

        foreach (var row in Selection.All())
        {
            if (!CanEditRow(row))
            {
                vm.UserQuestions.NoFullPrimaryKeyAsync().ListenErrors();
                return false;
            }
        }

        return true;
    }
    
    public override void UpdateSelectedCells(string? value)
    {
        if (RefreshInProgress)
            throw new Exception("Refresh is in progress!");

        if (!CanEditSelectedRow())
            return;
        
        foreach (var row in Selection.All())
        {
            if (row < 0 || row >= Count)
                continue;
            
            OverrideValue(row, SelectedCellIndex, value);
        }
        UpdateView();
    }

    public bool IsColumnPrimaryKey(int columnIndex) => columnsInfo[columnIndex].IsPrimaryKey;
}