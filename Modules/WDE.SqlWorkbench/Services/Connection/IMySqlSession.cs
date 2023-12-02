using System;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IMySqlSession : IMySqlQueryExecutor, IAsyncDisposable
{
    IConnection Connection { get; }
    bool IsConnected { get; }
    Task ScheduleAsync(Func<CancellationToken, IMySqlQueryExecutor, Task> action);
    Task CancelAllAsync();
    bool AnyTaskInSession();
}