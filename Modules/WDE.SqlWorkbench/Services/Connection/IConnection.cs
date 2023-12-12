using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Media;
using WDE.Common.Types;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IConnection : INotifyPropertyChanged
{
    DatabaseConnectionData ConnectionData { get; }
    Task<IMySqlSession> OpenSessionAsync();
    string ConnectionName { get; }
    ImageUri? Icon { get; }
    Color? Color { get; }
    bool IsRunning { get; }
    bool IsAutoCommit { get; }
    bool IsOpened { get; }
    int PendingTasksCount { get; }
    Task CancelAllAsync();
    Task SetAutoCommitAsync(bool autoCommit);
}