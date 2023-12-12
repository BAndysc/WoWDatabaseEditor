using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.ActionsOutput;
using WDE.SqlWorkbench.Services.UserQuestions;
using WDE.SqlWorkbench.Utils;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class SelectSingleTableViewModel : SelectResultsViewModel
{
    private readonly SqlWorkbenchViewModel vm;

    private SimpleSelect selectSpecs;
    private List<int> primaryKeyColumnsIndices;
    private List<ColumnInfo> columnsInfo;
    
    public bool RowsHaveFullPrimaryKey { get; }
    
    public ICommand SetSelectedToNullCommand { get; }
    public ICommand AddRowCommand { get; }
    public ICommand DuplicateRowCommand { get; }
    public ICommand DeleteRowCommand { get; }
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
        Title = this.selectSpecs.From.Table;
        
        var columnsDict = tableColumnsInfo.ToDictionary(c => c.Name);
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
        
        SetSelectedToNullCommand = new DelegateCommand(() => UpdateSelectedCells(null), () => RowsHaveFullPrimaryKey);
        AddRowCommand = new DelegateCommand(() =>
        {
            AdditionalRowsCount++;
            SelectedRowIndex = Count - 1;
            Selection.Clear();
            Selection.Add(SelectedRowIndex);
            for (int i = 0; i < Columns.Count; ++i)
            {
                OverrideValue(SelectedRowIndex, i, columnsInfo[i].DefaultValue?.ToString());
            }
        }, () => RowsHaveFullPrimaryKey);
        DuplicateRowCommand = new DelegateCommand(() =>
        {
            if (SelectedRowIndex < 0 || SelectedRowIndex >= Count)
                return;
            
            var copyRowIndex = SelectedRowIndex;
            
            AdditionalRowsCount++;
            SelectedRowIndex = Count - 1;
            Selection.Clear();
            Selection.Add(SelectedRowIndex);
            for (int i = 1; i < Columns.Count; ++i) // skip # column
                OverrideValue(SelectedRowIndex, i, GetFullValue(copyRowIndex, i));
        }, () => RowsHaveFullPrimaryKey);
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
        });
        DeleteRowCommand = new DelegateCommand(() =>
        {
            foreach (var row in Selection.All())
                MarkRowAsDeleted(row);
        }, () => RowsHaveFullPrimaryKey);
        ApplyChangesCommand = new AsyncAutoCommand(async () =>
        {
            try
            {
                ErrorIfConnectionChanged();
                await ApplyChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await vm.UserQuestions.SaveErrorAsync(e);   
            }
        });
        RevertChangesCommand = new AsyncAutoCommand(async () =>
        {
            ResetChanges();
        });
        RefreshTableCommand = new AsyncAutoCommand(async () =>
        {
            ErrorIfConnectionChanged();
            if (!await AskIfForgetChangesAsync())
                return;
            
            await RevertChangesCommand.ExecuteAsync();
            await vm.ExecuteAndOverrideResultsAsync(selectSpecs.ToString(), this);
        }).WrapMessageBox<Exception>(vm.UserQuestions);

        RowsHaveFullPrimaryKey = columnsInfo.Any(x => x.IsPrimaryKey) &&
                                 columnsInfo.Where(x => x.IsPrimaryKey)
                                     .All(x => Columns.Skip(1).Any(col => col.Header == x.Name));
    }
    
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

        foreach (var (alteredRow, values) in overrides)
        {
            if (IsRowMarkedAsDeleted(alteredRow))
                continue;
            
            if (alteredRow >= Results.AffectedRows) // inserts
                continue;
            
            var where = GenerateWhere(alteredRow);

            var set = string.Join(", ", values.Select(pair =>
            {
                var cellIndex = pair.Key;
                var value = pair.Value;

                var columnName = Results.ColumnNames[cellIndex - 1];
                value = ColumnValueToMySqlRepresentation(cellIndex, value);
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
    
    private async Task ApplyChangesAsync()
    {
        var changes = GenerateChanges();
        if (changes.Count == 0)
            return;
        changes.Insert(0, "START TRANSACTION");
        changes.Add("COMMIT");
        await vm.ExecuteAsync(changes, false, true, true);
        ResetChanges();
        Console.WriteLine( string.Join(";\n", changes));
    }
    
    public override async Task<bool> SaveAsync()
    {
        ErrorIfConnectionChanged();
        
        try
        {
            await ApplyChangesAsync();
            return true;
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

    private bool CanEditRow(int rowIndex) => rowIndex >= Results.AffectedRows || RowsHaveFullPrimaryKey;

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