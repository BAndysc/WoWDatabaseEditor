using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Services.TableUtils;

[SingleInstance]
[AutoRegister]
internal class QueryGenerator : IQueryGenerator
{
    private readonly IMySqlConnector mySqlConnector;

    public QueryGenerator(IMySqlConnector mySqlConnector)
    {
        this.mySqlConnector = mySqlConnector;
    }

    public async Task<string> GenerateSelectAllAsync(DatabaseCredentials credentials, string table, CancellationToken token)
    {
        await using var session = await mySqlConnector.ConnectAsync(credentials);
        var columns = await session.GetTableColumnsAsync(table, token);
        StringBuilder sb = new();
        sb.AppendLine("SELECT");
        sb.Append("    ");
        sb.AppendLine(string.Join(",\n    ", columns.Select(x => $"`{x.Name}`")));
        sb.AppendLine($"FROM `{credentials.SchemaName}`.`{table}`");
        return sb.ToString();
    }

    public async Task<string> GenerateInsertAsync(DatabaseCredentials credentials, string table, CancellationToken token)
    {
        await using var session = await mySqlConnector.ConnectAsync(credentials);
        var columns = await session.GetTableColumnsAsync(table, token);
        StringBuilder sb = new();
        sb.AppendLine($"INSERT INTO `{credentials.SchemaName}`.`{table}`");
        sb.Append("(");
        sb.Append(string.Join(", ", columns.Select(x => $"`{x.Name}`")));
        sb.AppendLine(")");
        sb.AppendLine("VALUES");
        sb.Append("(");
        sb.Append(string.Join(", ", columns.Select(x => $"<{x.Name}>")));
        sb.AppendLine(");");
        return sb.ToString();
    }

    public async Task<string> GenerateUpdateAsync(DatabaseCredentials credentials, string table, CancellationToken token)
    {
        await using var session = await mySqlConnector.ConnectAsync(credentials);
        var columns = await session.GetTableColumnsAsync(table, token);

        if (columns.All(x => !x.IsPrimaryKey))
            throw new NoPrimaryKeyColumnException(table);
        
        StringBuilder sb = new();
        sb.AppendLine($"UPDATE `{credentials.SchemaName}`.`{table}`");
        sb.AppendLine("SET");
        foreach (var col in columns)
            sb.AppendLine($"    `{col.Name}` = <{col.Name}>");

        sb.AppendLine("WHERE");
        var pkColumns = columns.Where(x => x.IsPrimaryKey).ToList();
        foreach (var col in pkColumns.SkipLast(1))
            sb.AppendLine($"    `{col.Name}` = <{col.Name}> AND");
        
        sb.AppendLine($"    `{pkColumns.Last().Name}` = <{pkColumns.Last().Name}>;");
        return sb.ToString();
    }

    public async Task<string> GenerateDeleteAsync(DatabaseCredentials credentials, string table, CancellationToken token)
    {
        await using var session = await mySqlConnector.ConnectAsync(credentials);
        var columns = await session.GetTableColumnsAsync(table, token);

        if (columns.All(x => !x.IsPrimaryKey))
            throw new NoPrimaryKeyColumnException(table);
        
        StringBuilder sb = new();
        sb.AppendLine($"DELETE FROM `{credentials.SchemaName}`.`{table}`");

        sb.AppendLine("WHERE");
        var pkColumns = columns.Where(x => x.IsPrimaryKey).ToList();
        foreach (var col in pkColumns.SkipLast(1))
            sb.AppendLine($"    `{col.Name}` = <{col.Name}> AND");
        
        sb.AppendLine($"    `{pkColumns.Last().Name}` = <{pkColumns.Last().Name}>;");
        return sb.ToString();
    }

    public async Task<string> GenerateCreateAsync(DatabaseCredentials credentials, string table, CancellationToken token)
    {
        await using var session = await mySqlConnector.ConnectAsync(credentials);
        return await session.GetCreateTableAsync(table, token) + ";";
    }

    public string GenerateDropTable(string? schemaName, string tableName)
    {
        return $"DROP TABLE `{schemaName}`.`{tableName}`;";
    }

    public string GenerateTruncateTable(string? schemaName, string tableName)
    {
        return $"TRUNCATE TABLE `{schemaName}`.`{tableName}`;";
    }
}