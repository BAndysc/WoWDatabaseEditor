using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.Mcp;
using WDE.Module.Attributes;

namespace WDE.Mcp.Tools.Sql;

public enum McpDatabaseKind
{
    World,
    Hotfix
}

public abstract class SqlToolBase<TInput, TOutput> : McpTool<TInput, TOutput> where TInput : class
{
    private readonly IMySqlExecutor worldExecutor;
    private readonly IMySqlHotfixExecutor hotfixExecutor;

    protected SqlToolBase(IMySqlExecutor worldExecutor, IMySqlHotfixExecutor hotfixExecutor)
    {
        this.worldExecutor = worldExecutor;
        this.hotfixExecutor = hotfixExecutor;
    }

    public override McpToolThreadMode ThreadMode => McpToolThreadMode.Background;

    protected (Func<string, Task<IDatabaseSelectResult>> select, Func<string, Task> execute, Func<Task<IList<string>>> tables, Func<string, Task<IList<MySqlDatabaseColumn>>> columns) Executor(McpDatabaseKind kind)
    {
        if (kind == McpDatabaseKind.Hotfix)
        {
            if (!hotfixExecutor.IsConnected)
                throw new McpToolException("Hotfix database is not connected (not available for this core?)");
            return (hotfixExecutor.ExecuteSelectSql, q => hotfixExecutor.ExecuteSql(q), hotfixExecutor.GetTables, hotfixExecutor.GetTableColumns);
        }

        if (!worldExecutor.IsConnected)
            throw new McpToolException("World database is not connected");
        return (worldExecutor.ExecuteSelectSql, q => worldExecutor.ExecuteSql(q), worldExecutor.GetTables, worldExecutor.GetTableColumns);
    }

    protected static async Task<T> GuardDatabaseErrors<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (IMySqlExecutor.DatabaseExecutorException e)
        {
            throw new McpToolException(e.InnerException?.Message ?? e.Message);
        }
    }
}

public sealed class SqlSelectInput
{
    [Description("A single SELECT (or SHOW/EXPLAIN/DESCRIBE) statement to run")]
    public required string Query { get; init; }

    [Description("Which database to query")]
    public McpDatabaseKind Database { get; init; } = McpDatabaseKind.World;

    [Description("Maximum number of rows to return (default 100)")]
    public int Limit { get; init; } = 100;
}

public sealed class SqlSelectOutput
{
    public required List<string> Columns { get; init; }
    public required List<List<object?>> Rows { get; init; }
    public required int TotalRows { get; init; }
    public required bool Truncated { get; init; }
}

[AutoRegister]
[SingleInstance]
public class SqlSelectTool : SqlToolBase<SqlSelectInput, SqlSelectOutput>
{
    private static readonly string[] AllowedPrefixes = { "select", "show", "explain", "describe", "desc", "with" };

    public SqlSelectTool(IMySqlExecutor worldExecutor, IMySqlHotfixExecutor hotfixExecutor)
        : base(worldExecutor, hotfixExecutor) { }

    public override string Name => "sql_select";
    public override string Description => "Runs a read-only SQL query against the editor's connected database and returns the resulting rows. Use table_list/table_describe first to discover the schema with friendly column meanings.";

    protected override async Task<SqlSelectOutput> Execute(SqlSelectInput input, CancellationToken token)
    {
        var trimmed = input.Query.TrimStart();
        if (!AllowedPrefixes.Any(p => trimmed.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            throw new McpToolException("Only SELECT/SHOW/EXPLAIN/DESCRIBE queries are allowed here; use sql_execute for mutations");

        var executor = Executor(input.Database);
        var result = await GuardDatabaseErrors(() => executor.select(input.Query));

        var columns = Enumerable.Range(0, result.Columns).Select(result.ColumnName).ToList();
        var limit = Math.Max(0, input.Limit);
        var rows = new List<List<object?>>();
        foreach (var rowIndex in result)
        {
            if (rows.Count >= limit)
                break;
            var row = new List<object?>(result.Columns);
            for (int col = 0; col < result.Columns; ++col)
            {
                var value = result.IsNull(rowIndex, col) ? null : result.Value(rowIndex, col);
                row.Add(value is null or bool or string || value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
                    ? value
                    : value.ToString());
            }
            rows.Add(row);
        }

        return new SqlSelectOutput
        {
            Columns = columns,
            Rows = rows,
            TotalRows = result.Rows,
            Truncated = result.Rows > rows.Count
        };
    }
}

public sealed class SqlExecuteInput
{
    [Description("SQL statement(s) to execute (INSERT/UPDATE/DELETE/REPLACE/...)")]
    public required string Query { get; init; }

    [Description("Which database to execute against")]
    public McpDatabaseKind Database { get; init; } = McpDatabaseKind.World;
}

[AutoRegister]
[SingleInstance]
public class SqlExecuteTool : SqlToolBase<SqlExecuteInput, string>
{
    public SqlExecuteTool(IMySqlExecutor worldExecutor, IMySqlHotfixExecutor hotfixExecutor)
        : base(worldExecutor, hotfixExecutor) { }

    public override string Name => "sql_execute";
    public override string Description => "DESTRUCTIVE: executes arbitrary SQL (INSERT/UPDATE/DELETE/...) directly against the editor's connected database, without undo. Prefer the *_generate_sql / *_update tools with execute=false to review SQL first.";
    public override bool Mutating => true;

    protected override async Task<string> Execute(SqlExecuteInput input, CancellationToken token)
    {
        var executor = Executor(input.Database);
        await GuardDatabaseErrors(async () =>
        {
            await executor.execute(input.Query);
            return true;
        });
        return "Query executed successfully";
    }
}

public sealed class SqlListRawTablesInput
{
    [Description("Which database to list tables from")]
    public McpDatabaseKind Database { get; init; } = McpDatabaseKind.World;
}

[AutoRegister]
[SingleInstance]
public class SqlListRawTablesTool : SqlToolBase<SqlListRawTablesInput, IList<string>>
{
    public SqlListRawTablesTool(IMySqlExecutor worldExecutor, IMySqlHotfixExecutor hotfixExecutor)
        : base(worldExecutor, hotfixExecutor) { }

    public override string Name => "sql_list_raw_tables";
    public override string Description => "Lists raw table names in the editor's connected database (physical schema; for friendly, editor-aware table info use table_list).";

    protected override Task<IList<string>> Execute(SqlListRawTablesInput input, CancellationToken token)
    {
        var executor = Executor(input.Database);
        return GuardDatabaseErrors(() => executor.tables());
    }
}

public sealed class SqlDescribeRawTableInput
{
    [Description("Raw table name")]
    public required string Table { get; init; }

    [Description("Which database the table lives in")]
    public McpDatabaseKind Database { get; init; } = McpDatabaseKind.World;
}

public sealed class RawColumnInfo
{
    public required string Name { get; init; }
    public required string DatabaseType { get; init; }
    public required bool Nullable { get; init; }
    public required bool PrimaryKey { get; init; }
    public object? DefaultValue { get; init; }
}

[AutoRegister]
[SingleInstance]
public class SqlDescribeRawTableTool : SqlToolBase<SqlDescribeRawTableInput, List<RawColumnInfo>>
{
    public SqlDescribeRawTableTool(IMySqlExecutor worldExecutor, IMySqlHotfixExecutor hotfixExecutor)
        : base(worldExecutor, hotfixExecutor) { }

    public override string Name => "sql_describe_raw_table";
    public override string Description => "Describes the physical columns of a raw database table (name, type, nullability, primary key).";

    protected override async Task<List<RawColumnInfo>> Execute(SqlDescribeRawTableInput input, CancellationToken token)
    {
        var executor = Executor(input.Database);
        var columns = await GuardDatabaseErrors(() => executor.columns(input.Table));
        return columns.Select(c => new RawColumnInfo
        {
            Name = c.ColumnName,
            DatabaseType = c.DatabaseType,
            Nullable = c.Nullable,
            PrimaryKey = c.PrimaryKey,
            DefaultValue = c.DefaultValue is null or bool or string ? c.DefaultValue : c.DefaultValue.ToString()
        }).ToList();
    }
}
