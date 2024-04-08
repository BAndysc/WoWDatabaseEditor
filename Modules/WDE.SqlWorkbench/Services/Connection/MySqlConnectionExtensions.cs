using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;

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

    public static async Task<IReadOnlyList<TableInfo>> GetTablesAsync(this IMySqlQueryExecutor conn, string schemaName, CancellationToken token = default, string? tableName = null)
    {
        var where = $"`TABLE_SCHEMA` = '{MySqlHelper.EscapeString(schemaName)}'";
        if (tableName != null)
            where += $" AND `TABLE_NAME` = '{MySqlHelper.EscapeString(tableName)}'";
        var results = await conn.ExecuteSqlAsync($"SELECT `TABLE_SCHEMA`, `TABLE_NAME`, `TABLE_TYPE`, `ENGINE`, `ROW_FORMAT`, `TABLE_COLLATION`, `DATA_LENGTH`, `TABLE_COMMENT` FROM `information_schema`.`TABLES` WHERE {where} ORDER BY `TABLE_NAME`;", null, token);

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
            .Select(i => new TableInfo(schemas[i]!, names[i]!, SqlParseUtils.ParseTableType(types[i]!), engines[i], SqlParseUtils.ParseRowFormat(rowFormats[i]), collations[i], dataLengths[i], comments[i]))
            .ToList();
    }
    
    public static async Task<IReadOnlyList<RoutineInfo>> GetRoutinesAsync(this IMySqlQueryExecutor conn, string schemaName, CancellationToken token)
    {
        var results = await conn.ExecuteSqlAsync($"SELECT `SPECIFIC_NAME`, `ROUTINE_SCHEMA`, `ROUTINE_TYPE`, `DATA_TYPE`, `DTD_IDENTIFIER`, `ROUTINE_DEFINITION`, `IS_DETERMINISTIC`, `SQL_DATA_ACCESS`, `SECURITY_TYPE`, `CREATED`, `LAST_ALTERED`, `ROUTINE_COMMENT`, `DEFINER` FROM `information_schema`.`routines` WHERE `ROUTINE_SCHEMA` = '{MySqlHelper.EscapeString(schemaName)}' ORDER BY `SPECIFIC_NAME`;", null, token);

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

    public static async Task<TableType?> GetTableTypeAsync(this IMySqlQueryExecutor conn, string? schema, string tableName, CancellationToken token)
    {
        string where = string.IsNullOrEmpty(schema) ? "" : $" IN `{MySqlHelper.EscapeString(schema)}`";
        var results = await conn.ExecuteSqlAsync($"SHOW FULL TABLES{where};", null, token);
        var tableNames = (StringColumnData)results.Columns[0]!;
        var tableTypes = (StringColumnData)results.Columns[1]!;
        
        for (int i = 0; i < results.AffectedRows; ++i)
        {
            if (tableNames[i] == tableName)
                return SqlParseUtils.ParseTableType(tableTypes[i]!);
        }

        return null;
    }
    
    public static async Task<IReadOnlyList<ColumnInfo>> GetTableColumnsAsync(this IMySqlQueryExecutor conn, string schema, string tableName, CancellationToken token)
    {
        List<ColumnInfo> columns = new();
        var databases = await conn.ExecuteSqlAsync($"SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = '{MySqlHelper.EscapeString(schema)}' AND `TABLE_NAME` = '{MySqlHelper.EscapeString(tableName)}' ORDER BY `ORDINAL_POSITION`", null, token);
        if (token.IsCancellationRequested)
            return columns;

        var names = (StringColumnData)databases["COLUMN_NAME"]!;
        var types = (StringColumnData)databases["COLUMN_TYPE"]!;
        var nullables = (StringColumnData)databases["IS_NULLABLE"]!;
        var keys = (StringColumnData)databases["COLUMN_KEY"]!;
        var defaults = (StringColumnData)databases["COLUMN_DEFAULT"]!;
        var extras = (StringColumnData)databases["EXTRA"]!;
        var charsets = (StringColumnData)databases["CHARACTER_SET_NAME"]!;
        var collations = (StringColumnData)databases["COLLATION_NAME"]!;
        for (int i = 0; i < databases.AffectedRows; ++i)
        {
            var column = new ColumnInfo(
                names[i]!,
                types[i]!,
                nullables[i] == "YES",
                keys[i] == "PRI",
                extras[i]!.Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                defaults[i],
                charsets[i],
                collations[i]);
            columns.Add(column);
        }

        return columns;
    }
    
    public static async Task<string> GetCreateTableAsync(this IMySqlQueryExecutor conn, string schema, string tableName, CancellationToken token = default)
    {
        var databases = await conn.ExecuteSqlAsync($"SHOW CREATE TABLE `{MySqlHelper.EscapeString(schema)}`.`{MySqlHelper.EscapeString(tableName)}`", null, token);
        if (token.IsCancellationRequested)
            return "";

        var createTable = (StringColumnData)databases["Create Table"]!;
        return createTable[0]!;
    }
    
    public static async Task<IReadOnlyList<TableEngine>> GetEnginesAsync(this IMySqlQueryExecutor conn, CancellationToken token = default)
    {
        var databases = await conn.ExecuteSqlAsync("SELECT `ENGINE`, `SUPPORT`, `COMMENT`, `TRANSACTIONS`, `XA`, `SAVEPOINTS` FROM `information_schema`.`ENGINES`", null, token);

        if (databases.IsNonQuery || databases.Columns.Length == 0)
            return Array.Empty<TableEngine>();

        List<TableEngine> engines = new();
        var engineNames = (StringColumnData)databases["ENGINE"]!;
        var engineSupports = (StringColumnData)databases["SUPPORT"]!;
        var engineComments = (StringColumnData)databases["COMMENT"]!;
        var engineTransactions = (StringColumnData)databases["TRANSACTIONS"]!;
        var engineXa = (StringColumnData)databases["XA"]!;
        var engineSavepoints = (StringColumnData)databases["SAVEPOINTS"]!;
        
        for (int i = 0; i < databases.AffectedRows; ++i)
        {
            engines.Add(new TableEngine(
                engineNames[i]!,
                engineSupports[i] == "DEFAULT",
                engineSupports[i] != "NO",
                engineComments[i]!,
                engineTransactions.IsNull(i) ? null : engineTransactions[i] == "YES",
                engineXa.IsNull(i) ? null : engineXa[i] == "YES",
                engineSavepoints.IsNull(i) ? null : engineSavepoints[i] == "YES"
            ));
        }
        return engines;
    }
    
    public static async Task<IReadOnlyList<Collation>> GetCollationsAsync(this IMySqlQueryExecutor conn, CancellationToken token = default)
    {
        var databases = await conn.ExecuteSqlAsync("SELECT `COLLATION_NAME`, `CHARACTER_SET_NAME`, `ID`, `IS_DEFAULT`, `IS_COMPILED` FROM `information_schema`.`COLLATIONS`", null, token);

        if (databases.IsNonQuery || databases.Columns.Length == 0)
            return Array.Empty<Collation>();

        List<Collation> collations = new();
        var collationNames = (StringColumnData)databases["COLLATION_NAME"]!;
        var collationCharsets = (StringColumnData)databases["CHARACTER_SET_NAME"]!;
        var collationIdsMySql = databases["ID"] as UInt64ColumnData;
        var collationIdsMaria = databases["ID"] as Int64ColumnData;
        var collationIsDefault = (StringColumnData)databases["IS_DEFAULT"]!;
        var collationIsCompiled = (StringColumnData)databases["IS_COMPILED"]!;
        
        for (int i = 0; i < databases.AffectedRows; ++i)
        {
            if (collationIdsMySql != null && collationIdsMySql[i] > long.MaxValue)
                throw new Exception("Type sizes mismatch. Please report this error (MySQL returns id as ulong, MariaDB as long, therefore the editor keeps it as long, but apparently your MySql just returned an id higher than max long).");
            
            collations.Add(new Collation(
                collationNames[i]!,
                collationCharsets[i]!,
                collationIdsMySql != null ? (long)collationIdsMySql[i] : (collationIdsMaria![i]),
                collationIsDefault[i] == "Yes",
                collationIsCompiled[i] == "Yes"
            ));
        }
        return collations;
    }

    public static async Task<Dictionary<string, List<ShowIndexEntry>>> GetIndexesAsync(this IMySqlQueryExecutor conn, string? schema, string table, CancellationToken token = default)
    {
        var from = $"`{MySqlHelper.EscapeString(table)}`";
        if (schema != null)
            from = $"`{MySqlHelper.EscapeString(schema)}`.{MySqlHelper.EscapeString(from)}";
        
        var results = await conn.ExecuteSqlAsync($"SHOW INDEX FROM {from}", null, token);

        if (results.IsNonQuery || results.Columns.Length == 0)
            return new Dictionary<string, List<ShowIndexEntry>>();
        
        //var tables = (StringColumnData)results["Table"]!;
        var nonUniquesMySql = results["Non_unique"] as Int32ColumnData;
        var nonUniquesMaria = results["Non_unique"] as Int64ColumnData;
        var keyNames = (StringColumnData)results["Key_name"]!;
        var seqInIndexesMySql = results["Seq_in_index"] as UInt32ColumnData;
        var seqInIndexesMaria = results["Seq_in_index"] as Int64ColumnData;
        var columnNames = (StringColumnData)results["Column_name"]!;
        var collations = (StringColumnData)results["Collation"]!;
        var subParts = (Int64ColumnData)results["Sub_part"]!;
        //var packeds = (ObjectColumnData)results["Packed"]!;
        var nulls = (StringColumnData)results["Null"]!;
        var indexTypes = (StringColumnData)results["Index_type"]!;
        var comments = (StringColumnData)results["Comment"]!;
        var indexComments = (StringColumnData)results["Index_comment"]!;
        var visibles = results.HasColumn("Visible") ? (StringColumnData)results["Visible"]! : null;
        //var expressions = (StringColumnData)results["Expression"]!;
        List<ShowIndexEntry> indexes = new();

        for (int i = 0; i < results.AffectedRows; ++i)
        {
            bool nonUnique = (nonUniquesMySql != null && nonUniquesMySql[i] == 1) || (nonUniquesMaria != null && nonUniquesMaria[i] == 1);
            string keyName = keyNames[i]!;
            long seqInIndex = seqInIndexesMySql != null ? seqInIndexesMySql[i] : seqInIndexesMaria![i];
            string columnName = columnNames[i]!;
            bool? ascending = collations.IsNull(i) ? null : collations[i] == "A";
            long? subPart = subParts.IsNull(i) ? null : subParts[i];
            bool canContainNull = nulls[i] == "YES";
            var indexType = indexTypes[i]!;
            var comment = comments[i];
            var indexComment = indexComments[i];
            var visible = visibles == null || visibles[i] == "YES";
            IndexKind kind = default;
            IndexType type = default;

            if (indexType == "BTREE" || indexType == "HASH")
            {
                type = indexType == "BTREE" ? IndexType.BTree : IndexType.Hash;
                kind = nonUnique ? IndexKind.NonUnique : IndexKind.Unique;
            }
            else if (indexType == "FULLTEXT")
                kind = IndexKind.FullText;
            else if (indexType == "RTREE")
                kind = IndexKind.Spatial;
            else
                throw new Exception("Unknown index type " + indexType);
            
            indexes.Add(new ShowIndexEntry(nonUnique, keyName, (int)seqInIndex, columnName, ascending, subPart!, canContainNull, kind, type, comment, indexComment, visible));
        }

        var grouped = indexes.GroupBy(x => x.KeyName)
            .ToDictionary(x => x.Key, x => x.OrderBy(x => x.SeqInIndex).ToList());
        
        return grouped;
    }
    
    public static async Task<string?> GetCurrentDatabaseAsync(this IMySqlQueryExecutor conn, CancellationToken token = default)
    {
        var databases = await conn.ExecuteSqlAsync("SELECT DATABASE()", null, token);
        if (databases.IsNonQuery || databases.Columns.Length == 0)
            return null;

        var database = (StringColumnData)databases.Columns[0]!;
        return database[0];
    }
}