using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using PropertyChanged.SourceGenerator;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels.SingleRow;

public partial class MySqlFilterViewModel : ObservableBase
{
    [AlsoNotify(nameof(ShowOperatorChoice))] [Notify] private FilterColumnViewModel? selectedColumn;
    [AlsoNotify(nameof(ShowFilterText))] [Notify] private FilterOperatorViewModel selectedOperator;
    [Notify] private string filterText = "";

    public bool ShowOperatorChoice => !selectedColumn?.IsManualQuery() ?? false;
    public bool ShowFilterText => selectedOperator.Operator is not ("IS NULL" or "IS NOT NULL");
    
    public List<FilterColumnViewModel> Columns { get; } = new();
    public List<FilterOperatorViewModel> Operators { get; } = new();
    public AsyncAutoCommand PickParameterCommand { get; }
    public AsyncAutoCommand ApplyFilter { get; }
    public FilterColumnViewModel RawSqlColumn { get; }
    public FilterColumnViewModel AnyColumn { get; }

    private List<(string tableName, string columnName, bool isString)> columnDatabaseNames;

    public MySqlFilterViewModel(DatabaseTableDefinitionJson? tableDefinition,
        Func<Task> refilter, 
        IParameterFactory parameterFactory,
        IParameterPickerService pickerService)
    {
        AnyColumn = FilterColumnViewModel.CreateAnyColumn();
        RawSqlColumn = FilterColumnViewModel.CreateRawSql();
        Columns.Add(AnyColumn);
        Columns.Add(RawSqlColumn);

        columnDatabaseNames = new();
        if (tableDefinition != null)
        {
            foreach (var column in tableDefinition.Groups.SelectMany(x => x.Fields))
            {
                if (!column.IsActualDatabaseColumn)
                    continue;

                var tableName = column.ForeignTable ?? tableDefinition.TableName;
                columnDatabaseNames.Add((tableName, column.DbColumnName, column.IsTypeString));
                Columns.Add(FilterColumnViewModel.CreateFromColumn(column.Name, column.DbColumnName, tableName, column.ValueType));
            }
        }
        
        Operators.Add(new FilterOperatorViewModel("="));
        Operators.Add(new FilterOperatorViewModel("!="));
        Operators.Add(new FilterOperatorViewModel("<"));
        Operators.Add(new FilterOperatorViewModel(">"));
        Operators.Add(new FilterOperatorViewModel("<="));
        Operators.Add(new FilterOperatorViewModel(">="));
        Operators.Add(new FilterOperatorViewModel("IN"));
        Operators.Add(new FilterOperatorViewModel("NOT IN"));
        Operators.Add(new FilterOperatorViewModel("IS NULL"));
        Operators.Add(new FilterOperatorViewModel("IS NOT NULL"));
        Operators.Add(new FilterOperatorViewModel("BETWEEN"));
        Operators.Add(new FilterOperatorViewModel("NOT BETWEEN"));
        Operators.Add(new FilterOperatorViewModel("LIKE", "contains"));
        Operators.Add(new FilterOperatorViewModel("NOT LIKE", "not contains"));

        selectedColumn = Columns[0];
        selectedOperator = Operators[0];
        
        ApplyFilter = new AsyncAutoCommand(refilter);
        PickParameterCommand = new AsyncAutoCommand(async () =>
        {
            if (parameterFactory.IsRegisteredLong(selectedColumn.ParameterKey!))
            {
                var param = parameterFactory.Factory(selectedColumn.ParameterKey!);
                var picked = await pickerService.PickParameter(param, 0);
                if (picked.ok)
                {
                    FilterText = picked.value.ToString();
                    await ApplyFilter.ExecuteAsync();
                }
            }
            else if (parameterFactory.IsRegisteredString(selectedColumn.ParameterKey!))
            {
                var param = parameterFactory.FactoryString(selectedColumn.ParameterKey!);
                var picked = await pickerService.PickParameter(param, "");
                if (picked.ok && picked.value != null)
                {
                    FilterText = picked.value;
                    await ApplyFilter.ExecuteAsync();
                }
            }
        }, () => selectedColumn != null && selectedColumn.ParameterKey != null && 
                 (parameterFactory.IsRegisteredLong(selectedColumn.ParameterKey!) || 
                 parameterFactory.IsRegisteredString(selectedColumn.ParameterKey!)));
        On(() => SelectedColumn, _ => PickParameterCommand.RaiseCanExecuteChanged());
        On(() => ShowFilterText, show =>
        {
            if (!show)
                ApplyFilter.Execute(null);
        });
    }

    private string BuildWhereForOperator(string tableName, string columnName, FilterOperatorViewModel @operator)
    {
        var where = "";
        var escaped = filterText.Replace("'", "\\'").Replace("%", "\\%");
        where = $"`{tableName}`.`{columnName}` ";
        if (@operator.Operator == "IS NULL")
            where += "IS NULL";
        else if (@operator.Operator == "IS NOT NULL")
            where += "IS NOT NULL";
        else
        {
            if (@operator.Operator == "LIKE")
                where += $"LIKE '%{escaped}%'";
            else if (@operator.Operator == "LIKE")
                where += $"NOT LIKE '%{escaped}%'";
            else if (@operator.Operator == "BETWEEN")
                where += $"BETWEEN {filterText}";
            else if (@operator.Operator == "NOT BETWEEN")
                where += $"NOT BETWEEN {filterText}";
            else
                where += $"{@operator.Operator} '{escaped}'";
        }

        return where;
    }
    
    public string BuildWhere()
    {
        if (selectedColumn == null)
            return "";

        if ((selectedColumn.IsManualQuery()) || string.IsNullOrEmpty(filterText))
            return filterText;

        if (selectedColumn.Type == FilterColumnType.ByColumn)
            return BuildWhereForOperator(selectedColumn.TableName!, selectedColumn.ColumnName!, selectedOperator);
        
        Debug.Assert(selectedColumn.Type == FilterColumnType.AnyColumn);

        List<string> wheres = new();
        var isFilterNumber = long.TryParse(filterText, out _) || float.TryParse(filterText, out _);
        foreach (var (table, column, isString) in columnDatabaseNames)
        {
            // we can't generate `column` = 'a string' is the column type is not string
            // because for number columns, the string will be converted to a number
            // which in mySql means - a zero. Any string will be converted to number 0
            // and the condition will be true for any number column that has value 0
            // just mysql things
            if (!isString && !isFilterNumber)
                continue;
            
            wheres.Add("(" + BuildWhereForOperator(table, column, selectedOperator) + ")");
        }

        return string.Join(" OR ", wheres);
    }
}

public enum FilterColumnType
{
    ByColumn,
    Raw,
    AnyColumn
}

public class FilterColumnViewModel
{
    public readonly string FriendlyName;
    public readonly string? ColumnName;
    public readonly string? TableName;
    public readonly string? ParameterKey;
    public readonly FilterColumnType Type;

    public static FilterColumnViewModel CreateFromColumn(string friendlyName, string name, string tableName, string parameterKey)
    {
        return new(FilterColumnType.ByColumn, name, friendlyName, tableName, parameterKey);
    }
    
    public static FilterColumnViewModel CreateRawSql()
    {
        return new(FilterColumnType.Raw, null, "Raw SQL", null, null);
    }
    
    public static FilterColumnViewModel CreateAnyColumn()
    {
        return new(FilterColumnType.AnyColumn, null, "Any column", null, null);
    }

    private FilterColumnViewModel(FilterColumnType type, string? columnName, string friendlyName, string? tableName, string? parameterKey)
    {
        Type = type;
        Debug.Assert((columnName != null) == (type == FilterColumnType.ByColumn));
        FriendlyName = friendlyName;
        ColumnName = columnName;
        TableName = tableName;
        ParameterKey = (parameterKey?.EndsWith("Parameter") ?? false) ? parameterKey : null;
    }
    
    public bool IsManualQuery() => Type == FilterColumnType.Raw;

    public bool IsAnyColumnQuery() => Type == FilterColumnType.AnyColumn;

    public override string ToString() => FriendlyName;
}

public class FilterOperatorViewModel
{
    public readonly string FriendlyName;
    public readonly string Operator;

    public FilterOperatorViewModel(string @operator, string? friendlyName = null)
    {
        this.FriendlyName = friendlyName ?? @operator;
        this.Operator = @operator;
    }

    public override string ToString() => FriendlyName;
}