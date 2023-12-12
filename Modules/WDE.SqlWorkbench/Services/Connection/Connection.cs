using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

internal class Connection : ObservableBase, IConnection
{
    private readonly IMySqlConnector connector;
    private readonly DatabaseConnectionData data;
    private IRawMySqlConnection? connection;
    private Task<IRawMySqlConnection>? connectionTask;
    private TaskQueue taskQueue;
    private int refCount = 0;
    private bool opened = false;
    private bool autoCommit = true; // mysql default

    public DatabaseConnectionData ConnectionData => data;

    public Connection(IMySqlConnector connector,
        DatabaseConnectionData data)
    {
        this.connector = connector;
        this.data = data;
        taskQueue = new();
        taskQueue.ToObservable(x => x.IsRunning).SubscribeAction(_ => RaisePropertyChanged(nameof(IsRunning)));
        taskQueue.ToObservable(x => x.PendingTasksCount).SubscribeAction(_ => RaisePropertyChanged(nameof(PendingTasksCount)));
    }

    public Color? Color => data.Color;
    public bool IsRunning => taskQueue.IsRunning;
    public bool IsAutoCommit => autoCommit;
    public bool IsOpened => opened;
    public int PendingTasksCount => taskQueue.PendingTasksCount;
    public string ConnectionName => data.ConnectionName;
    public ImageUri? Icon => data.Icon;

    public async Task<IMySqlSession> OpenSessionAsync()
    {
        if (connection != null)
        {
            refCount++;
            return new Session(this);
        }

        if (connectionTask != null)
        {
            refCount++;
            await connectionTask;
            return new Session(this);
        }

        var tcs = new TaskCompletionSource<IRawMySqlConnection>();
        connectionTask = tcs.Task;
        
        refCount++;
        connection = await connector.ConnectAsync(data.Credentials, data.SafeMode);
        opened = true;
        RaisePropertyChanged(nameof(IsOpened));
        autoCommit = true;
        tcs.SetResult(connection);
        connectionTask = null;
        return new Session(this);
    }

    public async Task CancelAllAsync()
    {
        await taskQueue.CancelAll();
    }

    private async Task ReleaseAsync(Session session)
    {
        bool lastSession = --refCount == 0;
        var thisConnection = connection!;

        if (lastSession)
        {
            connection = null;
            opened = false;
            RaisePropertyChanged(nameof(IsOpened));

            await taskQueue.CancelAll();
            thisConnection.Dispose();                
        }
        else
        {
            await taskQueue.CancelGroup(session);
        }
    }

    public async Task SetAutoCommitAsync(bool autoCommit)
    {
        if (connection == null)
            return;
        
        await connection.ExecuteSqlAsync("SET autocommit = " + (autoCommit ? "ON" : "OFF"));
        this.autoCommit = autoCommit;
        RaisePropertyChanged(nameof(IsAutoCommit));
    }
    
    public IConnection Clone(string schemaName)
    {
        return new Connection(connector, data.WithSchemaName(schemaName));
    }

    private class Session : IMySqlSession
    {
        private readonly Connection owner;
        private bool disposed = false;
        
        public IConnection Connection => owner;
        
        public bool IsConnected => owner.connection?.IsSessionOpened ?? false;

        public Session(Connection owner)
        {
            this.owner = owner;
        }
        
        public async ValueTask DisposeAsync()
        {
            if (disposed)
                return;
            
            disposed = true;
            await owner.ReleaseAsync(this);
        }

        public async Task ScheduleAsync(Func<CancellationToken, IMySqlQueryExecutor, Task> action)
        {
            if (disposed)
                throw new ObjectDisposedException("Session is disposed");

            await owner.taskQueue.Schedule(async taskToken =>
            {
                await action(taskToken, owner.connection!);
            }, this);
        }

        public async Task CancelAllAsync()
        {
            if (disposed)
                throw new ObjectDisposedException("Session is disposed");
            
            await owner.taskQueue.CancelGroup(this);
        }

        public bool AnyTaskInSession()
        {
            return owner.taskQueue.IsAnyTaskPendingOrRunning(this);
        }

        public async Task<SelectResult> ExecuteSqlAsync(string query, int? rowsLimit = null, CancellationToken token = default)
        {
            if (disposed)
                throw new ObjectDisposedException("Session is disposed");
            
            return await owner.taskQueue.Schedule(async taskToken =>
            {
                if (disposed)
                    throw new ObjectDisposedException("Session is disposed");
                
                return await owner.connection!.ExecuteSqlAsync(query, rowsLimit, taskToken);
            }, this);
        }
    }
}