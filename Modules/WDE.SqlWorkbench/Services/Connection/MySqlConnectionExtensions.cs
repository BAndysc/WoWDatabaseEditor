using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

internal static class MySqlConnectionExtensions
{
    public static async Task<IReadOnlyList<string>> GetDatabasesAsync(this IMySqlQueryExecutor conn, CancellationToken token = default)
    {
        var databases = await conn.ExecuteSqlAsync("SHOW DATABASES", null, token);

        if (databases.IsNonQuery || databases.Columns.Length == 0)
            return Array.Empty<string>();

        return Enumerable.Range(0, databases.AffectedRows)
            .Select(x => databases.Columns[0]!.GetToString(x)!)
            .ToList();
    }

    public static async Task<IReadOnlyList<TableInfo>> GetTablesAsync(this IMySqlQueryExecutor conn, string schemaName, CancellationToken token = default)
    {
        var results = await conn.ExecuteSqlAsync($"SELECT `TABLE_SCHEMA`, `TABLE_NAME`, `TABLE_TYPE`, `ENGINE`, `ROW_FORMAT`, `TABLE_COLLATION`, `DATA_LENGTH`, `TABLE_COMMENT` FROM `information_schema`.`TABLES` WHERE `TABLE_SCHEMA` = '{schemaName}';", null, token);

        if (results.IsNonQuery || results.Columns.Length == 0)
            return Array.Empty<TableInfo>();

        var schemas = (StringColumnData)results.Columns[0]!;
        var names = (StringColumnData)results.Columns[1]!;
        var types = (StringColumnData)results.Columns[2]!;
        var engines = (StringColumnData)results.Columns[3]!;
        var rowFormats = (StringColumnData)results.Columns[4]!;
        var collations = (StringColumnData)results.Columns[5]!;
        var dataLengths = (UInt64ColumnData)results.Columns[6]!;
        var comments = (StringColumnData)results.Columns[7]!;
        
        return Enumerable.Range(0, results.AffectedRows)
            .Select(i => new TableInfo(schemas[i]!, names[i]!, SqlParseUtils.ParseTableType(types[i]!), engines[i], rowFormats[i], collations[i], dataLengths[i], comments[i]))
            .ToList();
    }

    public static async Task<IReadOnlyList<RoutineInfo>> GetRoutinesAsync(this IMySqlQueryExecutor conn, string schemaName, CancellationToken token)
    {
        var results = await conn.ExecuteSqlAsync($"SELECT `SPECIFIC_NAME`, `ROUTINE_SCHEMA`, `ROUTINE_TYPE`, `DATA_TYPE`, `DTD_IDENTIFIER`, `ROUTINE_DEFINITION`, `IS_DETERMINISTIC`, `SQL_DATA_ACCESS`, `SECURITY_TYPE`, `CREATED`, `LAST_ALTERED`, `ROUTINE_COMMENT`, `DEFINER` FROM `information_schema`.`routines` WHERE `ROUTINE_SCHEMA` = '{schemaName}' ORDER BY `SPECIFIC_NAME`;", null, token);

        if (results.IsNonQuery || results.Columns.Length == 0)
            return Array.Empty<RoutineInfo>();
        
        var names = (StringColumnData)results.Columns[0]!;
        var schemas = (StringColumnData)results.Columns[1]!;
        var types = (StringColumnData)results.Columns[2]!;
        var functionReturnTypes = (StringColumnData)results.Columns[3]!;
        var functionFullReturnTypes = (StringColumnData)results.Columns[4]!;
        var bodies = (StringColumnData)results.Columns[5]!;
        var isDeterministics = (StringColumnData)results.Columns[6]!;
        var dataAccessTypes = (StringColumnData)results.Columns[7]!;
        var securityTypes = (StringColumnData)results.Columns[8]!;
        var createds = (MySqlDateTimeColumnData)results.Columns[9]!;
        var lastAltered = (MySqlDateTimeColumnData)results.Columns[10]!;
        var comments = (StringColumnData)results.Columns[11]!;
        var definers = (StringColumnData)results.Columns[12]!;

        RoutineInfo[] routines = new RoutineInfo[results.AffectedRows];
        for (int i = 0; i < results.AffectedRows; ++i)
        {
            routines[i] = new RoutineInfo(
                schemas[i]!,
                names[i]!,
                SqlParseUtils.ParseRoutineType(types[i]!),
                functionReturnTypes[i],
                functionFullReturnTypes[i],
                bodies[i],
                isDeterministics[i] == "YES",
                SqlParseUtils.ParseSqlDataAccessType(dataAccessTypes[i]!),
                SqlParseUtils.ParseSecurityType(securityTypes[i]!),
                createds[i],
                lastAltered[i],
                comments[i],
                definers[i]
            );
        }
        return routines;
    }

    public static async Task<IReadOnlyList<ColumnInfo>> GetTableColumnsAsync(this IMySqlQueryExecutor conn, string? schema, string tableName, CancellationToken token)
    {
        List<ColumnInfo> columns = new();
        if (!tableName.StartsWith('`'))
            tableName = $"`{tableName}`";

        var from = "";
        if (schema != null)
            from = $"`{schema}`.";

        from += tableName;
        
        var databases = await conn.ExecuteSqlAsync($"SHOW COLUMNS FROM {from}", null, token);
        if (token.IsCancellationRequested)
            return columns;

        var names = (StringColumnData)databases["Field"]!;
        var types = (StringColumnData)databases["Type"]!;
        var nullables = (StringColumnData)databases["Null"]!;
        var keys = (StringColumnData)databases["Key"]!;
        var defaults = (StringColumnData)databases["Default"]!;
        var extras = (StringColumnData)databases["Extra"]!;
        for (int i = 0; i < databases.AffectedRows; ++i)
        {
            var column = new ColumnInfo(
                names[i]!,
                types[i]!,
                nullables[i] == "YES",
                keys[i] == "PRI",
                extras[i]!.Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                defaults[i]);
            columns.Add(column);
        }

        return columns;
    }
    
    public static async Task<string> GetCreateTableAsync(this IMySqlQueryExecutor conn, string schema, string tableName, CancellationToken token = default)
    {
        var databases = await conn.ExecuteSqlAsync($"SHOW CREATE TABLE `{schema}`.`{tableName}`", null, token);
        if (token.IsCancellationRequested)
            return "";

        var createTable = (StringColumnData)databases["Create Table"]!;
        return createTable[0]!;
    }
}