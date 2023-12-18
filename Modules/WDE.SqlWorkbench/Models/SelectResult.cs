using System;

namespace WDE.SqlWorkbench.Models;

internal readonly struct SelectResult
{
    public readonly string[] ColumnNames;
    public readonly Type?[] ColumnTypes;
    public readonly IReadOnlyColumnData?[] Columns;
    public readonly int AffectedRows;

    public readonly bool IsNonQuery;
    
    private SelectResult(string[]? columnNames, Type?[]? columnTypes, IReadOnlyColumnData[]? columns, int affectedRows = -1, bool nonQuery = false)
    {
        ColumnNames = columnNames ?? Array.Empty<string>();
        ColumnTypes = columnTypes ?? Array.Empty<Type>();
        Columns = columns ?? Array.Empty<IReadOnlyColumnData>();
        AffectedRows = affectedRows;
        IsNonQuery = nonQuery;
    }

    public IReadOnlyColumnData? this[string columnName]
    {
        get
        {
            var index = Array.IndexOf(ColumnNames, columnName);
            if (index == -1)
                throw new Exception($"Column {columnName} not found");
            return Columns[index];
        }
    }
    
    public bool HasColumn(string columnName) => Array.IndexOf(ColumnNames, columnName) != -1;

    public static SelectResult NonQuery(int affectedRows)
    {
        return new SelectResult(null, null, null, affectedRows, true);
    }
    
    public static SelectResult Query(string[] columnNames, Type?[] columnTypes, int rows, IReadOnlyColumnData[] columns)
    {
        return new SelectResult(columnNames, columnTypes, columns, rows, false);
    }
}
