using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IMySqlSession : System.IDisposable, System.IAsyncDisposable
{
    bool IsSessionOpened { get; }
    Task<bool> TryReconnectAsync();
    Task<SelectResult> ExecuteSqlAsync(string query, int? rowsLimit = null, CancellationToken token = default);
    Task<IReadOnlyList<string>> GetDatabasesAsync(CancellationToken token = default);
    Task<IReadOnlyList<string>> GetTablesAsync(CancellationToken token = default);
    Task<IReadOnlyList<ColumnInfo>> GetTableColumnsAsync(string tableName, CancellationToken token = default);
    Task<string> GetCreateTableAsync(string tableName, CancellationToken token = default);
}