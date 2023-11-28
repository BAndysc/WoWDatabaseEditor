using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.TableUtils;

[UniqueProvider]
internal interface IQueryGenerator
{
    Task<string> GenerateSelectAllAsync(DatabaseCredentials credentials, string table, CancellationToken token = default);
    Task<string> GenerateInsertAsync(DatabaseCredentials credentials, string table, CancellationToken token = default);
    Task<string> GenerateUpdateAsync(DatabaseCredentials credentials, string table, CancellationToken token = default);
    Task<string> GenerateDeleteAsync(DatabaseCredentials credentials, string table, CancellationToken token = default);
    Task<string> GenerateCreateAsync(DatabaseCredentials credentials, string table, CancellationToken token = default);
    string GenerateDropTable(string? schemaName, string tableName);
    string GenerateTruncateTable(string? schemaName, string tableName);
}