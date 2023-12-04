using System.Threading;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IRawMySqlConnection : IMySqlQueryExecutor, System.IDisposable, System.IAsyncDisposable
{
    bool IsSessionOpened { get; }
}

internal interface IMySqlQueryExecutor
{
    Task<SelectResult> ExecuteSqlAsync(string query, int? rowsLimit = null, CancellationToken token = default);
}