using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Services.TableUtils;

[UniqueProvider]
internal interface IQueryGenerator
{
    Task<string> GenerateSelectAllAsync(IMySqlQueryExecutor executor, string schema, string table, CancellationToken token = default);
    Task<string> GenerateInsertAsync(IMySqlQueryExecutor executor, string schema, string table, CancellationToken token = default);
    Task<string> GenerateUpdateAsync(IMySqlQueryExecutor executor, string schema, string table, CancellationToken token = default);
    Task<string> GenerateDeleteAsync(IMySqlQueryExecutor executor, string schema, string table, CancellationToken token = default);
    Task<string> GenerateCreateAsync(IMySqlQueryExecutor executor, string schema, string table, CancellationToken token = default);
    string GenerateDropTable(string? schemaName, string tableName, TableType tableType);
    string GenerateTruncateTable(string? schemaName, string tableName);
}