using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PropertyChanged.SourceGenerator;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels.SingleRow;

public partial class MySqlFilterViewModel : ObservableBase
{
    [AlsoNotify(nameof(ShowOperatorChoice))] [Notify] private FilterColumnViewModel? selectedColumn;
    [Notify] private FilterOperatorViewModel? selectedOperator;
    [Notify] private string filterText = "";

    public bool ShowOperatorChoice => !selectedColumn?.IsManualQuery() ?? false;
    
    public List<FilterColumnViewModel> Columns { get; } = new();
    public List<FilterOperatorViewModel> Operators { get; } = new();

    public AsyncAutoCommand ApplyFilter { get; }
    
    public MySqlFilterViewModel(DatabaseTableDefinitionJson? tableDefinition, Func<Task> refilter)
    {
        Columns.Add(FilterColumnViewModel.CreateRawSql());

        if (tableDefinition != null)
        {
            foreach (var column in tableDefinition.Groups.SelectMany(x => x.Fields))
            {
                Columns.Add(FilterColumnViewModel.CreateFromColumn(column.Name, column.DbColumnName, column.ForeignTable ?? tableDefinition.TableName));
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
    }

    public string BuildWhere()
    {
        var where = "";
        if ((selectedColumn?.IsManualQuery() ?? true) || selectedOperator == null || string.IsNullOrEmpty(filterText))
        {
            where = filterText;
        }
        else
        {
            var escaped = filterText.Replace("'", "\\'").Replace("%", "\\%");
            where = $"`{selectedColumn.TableName}`.`{selectedColumn.ColumnName}` ";
            if (selectedOperator.Operator == "LIKE")
                where += $"LIKE '%{escaped}%'";
            else if (selectedOperator.Operator == "LIKE")
                where += $"NOT LIKE '%{escaped}%'";
            else if (selectedOperator.Operator == "BETWEEN")
                where += $"BETWEEN {filterText}";
            else if (selectedOperator.Operator == "NOT BETWEEN")
                where += $"NOT BETWEEN {filterText}";
            else if (selectedOperator.Operator == "IS NULL")
                where += $"IS NULL";
            else if (selectedOperator.Operator == "IS NOT NULL")
                where += $"IS NOT NULL";
            else
                where += $"{selectedOperator.Operator} '{escaped}'";
        }

        return where;
    }
}

public class FilterColumnViewModel
{
    public readonly string FriendlyName;
    public readonly string? ColumnName;
    public readonly string? TableName;
    
    public static FilterColumnViewModel CreateFromColumn(string friendlyName, string name, string tableName)
    {
        return new(name, friendlyName, tableName);
    }
    
    public static FilterColumnViewModel CreateRawSql()
    {
        return new(null, "Raw SQL", null);
    }

    private FilterColumnViewModel(string? columnName, string friendlyName, string? tableName)
    {
        this.FriendlyName = friendlyName;
        this.ColumnName = columnName;
        TableName = tableName;
    }
    
    public bool IsManualQuery()
    {
        return ColumnName == null;
    }

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