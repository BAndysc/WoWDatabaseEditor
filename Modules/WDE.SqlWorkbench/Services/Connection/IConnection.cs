using System.ComponentModel;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IConnection : INotifyPropertyChanged
{
    DatabaseConnectionData ConnectionData { get; }
    Task<IMySqlSession> OpenSessionAsync();
    bool IsRunning { get; }
    bool IsAutoCommit { get; }
    Task CancelAllAsync();
    Task SetAutoCommitAsync(bool autoCommit);
    IConnection Clone(string schemaName);
}