using System.ComponentModel;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IConnection : INotifyPropertyChanged
{
    DatabaseConnectionData ConnectionData { get; }
    Task<IMySqlSession> OpenSessionAsync();
    string ConnectionName { get; }
    bool IsRunning { get; }
    bool IsAutoCommit { get; }
    bool IsOpened { get; }
    int PendingTasksCount { get; }
    Task CancelAllAsync();
    Task SetAutoCommitAsync(bool autoCommit);
}