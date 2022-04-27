using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public MySqlFilterViewModel(DatabaseTableDefinitionJson? tableDefinition,
        Func<Task> refilter, 
        IParameterFactory parameterFactory,
        IParameterPickerService pickerService)
    {
        RawSqlColumn = FilterColumnViewModel.CreateRawSql();
        Columns.Add(RawSqlColumn);

        if (tableDefinition != null)
        {
            foreach (var column in tableDefinition.Groups.SelectMany(x => x.Fields))
            {
                Columns.Add(FilterColumnViewModel.CreateFromColumn(column.Name, column.DbColumnName, column.ForeignTable ?? tableDefinition.TableName, column.ValueType));
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

    public string BuildWhere()
    {
        var where = "";
        var escaped = filterText.Replace("'", "\\'").Replace("%", "\\%");
        where = $"`{selectedColumn?.TableName}`.`{selectedColumn?.ColumnName}` ";
        if (selectedOperator.Operator == "IS NULL")
            where += "IS NULL";
        else if (selectedOperator.Operator == "IS NOT NULL")
            where += "IS NOT NULL";
        else if ((selectedColumn?.IsManualQuery() ?? true) || string.IsNullOrEmpty(filterText))
        {
            where = filterText;
        }
        else
        {
            if (selectedOperator.Operator == "LIKE")
                where += $"LIKE '%{escaped}%'";
            else if (selectedOperator.Operator == "LIKE")
                where += $"NOT LIKE '%{escaped}%'";
            else if (selectedOperator.Operator == "BETWEEN")
                where += $"BETWEEN {filterText}";
            else if (selectedOperator.Operator == "NOT BETWEEN")
                where += $"NOT BETWEEN {filterText}";
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
    public readonly string? ParameterKey;

    public static FilterColumnViewModel CreateFromColumn(string friendlyName, string name, string tableName, string parameterKey)
    {
        return new(name, friendlyName, tableName, parameterKey);
    }
    
    public static FilterColumnViewModel CreateRawSql()
    {
        return new(null, "Raw SQL", null, null);
    }

    private FilterColumnViewModel(string? columnName, string friendlyName, string? tableName, string? parameterKey)
    {
        this.FriendlyName = friendlyName;
        this.ColumnName = columnName;
        TableName = tableName;
        this.ParameterKey = (parameterKey?.EndsWith("Parameter") ?? false) ? parameterKey : null;
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