using System;

namespace WDE.SqlWorkbench.Models;

internal static class SqlParseUtils
{
    public static TableType ParseTableType(string type)
    {
        return type switch
        {
            "BASE TABLE" => TableType.Table,
            "VIEW" => TableType.View,
            "SYSTEM VIEW" => TableType.SystemView,
            _ => throw new ArgumentOutOfRangeException($"Table type {type} is not known.")
        };
    }
    
    public static RoutineType ParseRoutineType(string type)
    {
        return type switch
        {
            "PROCEDURE" => RoutineType.Procedure,
            "FUNCTION" => RoutineType.Function,
            _ => throw new ArgumentOutOfRangeException($"Routine type {type} is not known.")
        };
    }
    
    public static SecurityType ParseSecurityType(string type)
    {
        return type switch
        {
            "DEFINER" => SecurityType.Definer,
            "INVOKER" => SecurityType.Invoker,
            _ => throw new ArgumentOutOfRangeException($"Security type {type} is not known.")
        };
    }
    
    public static SqlDataAccessType ParseSqlDataAccessType(string type)
    {
        return type switch
        {
            "CONTAINS SQL" => SqlDataAccessType.ContainsSql,
            "NO SQL" => SqlDataAccessType.NoSql,
            "READS SQL DATA" => SqlDataAccessType.ReadsSqlData,
            "MODIFIES SQL DATA" => SqlDataAccessType.ModifiesSqlData,
            _ => throw new ArgumentOutOfRangeException($"Data access type {type} is not known.")
        };
    }
    
    public static RowFormat? ParseRowFormat(string? rowFormat)
    {
        return rowFormat?.ToLower() switch
        {
            null => null,
            "fixed" => RowFormat.Fixed,
            "dynamic" => RowFormat.Dynamic,
            "compressed" => RowFormat.Compressed,
            "redundant" => RowFormat.Redundant,
            "compact" => RowFormat.Compact,
            "paged" => RowFormat.Paged,
            "page" => RowFormat.Page,
            _ => throw new ArgumentOutOfRangeException($"Row format {rowFormat} is not known.")
        };
    }
}