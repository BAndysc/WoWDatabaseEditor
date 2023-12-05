using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using MySqlConnector;
using Prism.Commands;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.QueryUtils;
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
    public ICommand CopyInsertCommand { get; }
    public IAsyncCommand RefreshCommand { get; }
    public IAsyncCommand ApplyChangesCommand { get; }
    public IAsyncCommand RevertChangesCommand { get; }
    public IAsyncCommand CreateViewCommand { get; }
    public IAsyncCommand PasteSelectedCommand { get; }

    public override ImageUri Icon => new("Icons/icon_mini_table_big.png");
    
    private int sortByIndex = -1;
    
    public SelectSingleTableViewModel(SqlWorkbenchViewModel vm,
        string title,
        in SelectResult results,
        SimpleSelect selectSpecs, 
        IReadOnlyList<ColumnInfo> tableColumnsInfo) : base(vm, title, results)
    {
        this.vm = vm;
        this.selectSpecs = selectSpecs;
        primaryKeyColumnsIndices = new List<int>();
        columnsInfo = new List<ColumnInfo>();
        
        var columnsDict = tableColumnsInfo.ToDictionary(c => c.Name);
        columnsInfo.Add(new ColumnInfo("#", "", false, false, false, null));
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
            while (tokenizer.HasNextLine())
            {
                tokenizer.NextLine();
                int rowIndex = AdditionalRowsCount++;
                
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
        CopyInsertCommand = new DelegateCommand(() =>
        {
            if (TryGenerateInsert(Selection.All(), false, out var insert))
                vm.ClipboardService.SetText(insert);
        });
        ApplyChangesCommand = new AsyncAutoCommand(async () =>
        {
            try
            {
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
        RefreshCommand = new AsyncAutoCommand(async () =>
        {
            if (!await AskIfForgetChangesAsync())
                return;
            
            await RevertChangesCommand.ExecuteAsync();
            await vm.ExecuteAndOverrideResultsAsync(selectSpecs.ToString(), this);
        }, () => RowsHaveFullPrimaryKey);
        CreateViewCommand = new AsyncAutoCommand(async () =>
        {
            var newViewName = await vm.UserQuestions.AskForNewViewNameAsync();
            if (string.IsNullOrWhiteSpace(newViewName))
                return;
            
            try
            { 
                if (!string.IsNullOrEmpty(selectSpecs.From.Schema))
                    newViewName = $"`{selectSpecs.From.Schema}`.`{newViewName}`";
                else
                    newViewName = $"`{newViewName}`";
                await vm.ExecuteAsync(new[] { $"CREATE VIEW {newViewName} AS {selectSpecs.ToString()}" }, false, true, false);
            }
            catch (Exception e)
            {
                await vm.UserQuestions.SaveErrorAsync(e);
            }
        });

        RowsHaveFullPrimaryKey = columnsInfo.Any(x => x.IsPrimaryKey) &&
                                 columnsInfo.Where(x => x.IsPrimaryKey)
                                     .All(x => Columns.Skip(1).Any(col => col.Header == x.Name));
    }
    
    private string ColumnValueToMySqlRepresentation(int columnIndex, string? value)
    {
        if (value == null)
            return "NULL";

        var columnCategory = Results.Columns[columnIndex - 1]?.Category ?? ColumnTypeCategory.Unknown;

        if (columnCategory == ColumnTypeCategory.Unknown)
        {
            var columnName = Results.ColumnNames[columnIndex - 1];
            throw new Exception($"The column {columnName} has type {Results.Columns[columnIndex - 1]?.GetType()} which editor can't edit right now. Please report this.");
        }
            
        if (columnCategory is ColumnTypeCategory.String)
            return $"'{MySqlHelper.EscapeString(value)}'";
        else if (columnCategory is ColumnTypeCategory.DateTime)
        {
            if (value.Contains('(') && value.Contains(')')) // contains a function call
                return value;
            return $"'{value}'";
        }
        else if (columnCategory is ColumnTypeCategory.Binary)
        {
            return $"X'{value}'";
        }
            
        return value;
    }

    private bool TryGenerateInsert(IEnumerable<int> rows, bool skipDeleted, out string insert)
    {
        List<string> inserts = new();
        foreach (var rowIndex in rows)
        {
            if (skipDeleted && IsRowMarkedAsDeleted(rowIndex))
                continue;
            
            List<string> columns = new();
            for (int columnIndex = 1; columnIndex < Columns.Count; ++columnIndex)
                columns.Add(ColumnValueToMySqlRepresentation(columnIndex, GetFullValue(rowIndex, columnIndex)));
            inserts.Add("(" + string.Join(", ", columns) + ")");
        }

        if (inserts.Count > 0)
        {
            var columns = string.Join(", ", Results.ColumnNames.Select(x => $"`{x}`"));
            insert = $"INSERT INTO {selectSpecs.From} ({columns}) VALUES\n" + string.Join(",\n", inserts);
            return true;
        }

        insert = "";
        return false;
    }
    
    private List<string> GenerateChanges()
    {
        string GenerateWhere(int rowIndex)
        {
            return string.Join(" AND ", primaryKeyColumnsIndices.Select(columnIndex =>
            {
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

    public void UpdateResults(in SelectResult results)
    {
        Results = results;
        UpdateView();
        RaisePropertyChanged(nameof(Count));
        RaisePropertyChanged(nameof(Results));
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