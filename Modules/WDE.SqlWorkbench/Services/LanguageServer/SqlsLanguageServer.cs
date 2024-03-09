using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using WDE.Common;
using WDE.Common.Services.Processes;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Settings;

namespace WDE.SqlWorkbench.Services.LanguageServer;

[AutoRegister]
[SingleInstance]
internal class SqlsLanguageServer : ISqlLanguageServer
{
    private static readonly TimeSpan MaxCrashTimeWindow = TimeSpan.FromSeconds(60);
    private static int MaxCrashCount = 5;
    
    private readonly ISqlWorkbenchPreferences preferences;
    private readonly IProgramFinder programFinder;
    private List<bool> disposedClients = new();
    private List<LanguageClient> clients = new();
    private List<Process?> processes = new();
    private List<DatabaseCredentials> credentials = new();
    private List<Guid> guids = new();
    private List<List<SqlsLanguageServerFile>> filesPerClient = new();
    private Dictionary<Guid, LanguageServerConnectionId> guidToConnId = new();
    private Dictionary<LanguageServerConnectionId, Task> connectionTasks = new(); 
    private Dictionary<LanguageServerConnectionId, (DateTime lastCrash, int crashCount)> lastCrashStats = new();
    private long fileId = 0;

    public SqlsLanguageServer(ISqlWorkbenchPreferences preferences,
        IProgramFinder programFinder)
    {
        this.preferences = preferences;
        this.programFinder = programFinder;
    }
    
    private async Task RecreateClientAsync(LanguageServerConnectionId connectionId)
    {
        if (disposedClients[connectionId.Id])
            return;
        
        var newClient = await CreateClientAsync(connectionId);
        foreach (var file in filesPerClient[connectionId.Id])
        {
            file.Reconnect(newClient);
        }
    }

    private string? LocateSqls()
    {
        if (preferences.CustomSqlsPath != null &&
            File.Exists(preferences.CustomSqlsPath))
            return preferences.CustomSqlsPath;
        
        return programFinder.TryLocate("sqls", "sqls/sqls.exe");
    }
    
    private void ForeachFile(LanguageServerConnectionId connId, Action<SqlsLanguageServerFile> action)
    {
        foreach (var file in filesPerClient[connId.Id])
        {
            action(file);
        }
    }

    private async Task<LanguageClient> CreateClientAsync(LanguageServerConnectionId connectionId)
    {
        var sqlPath = LocateSqls();
        
        if (sqlPath == null)
            throw new Exception("Can't find sqls executable");
        
        ForeachFile(connectionId, f => f.NotifyLanguageServerAlive());
        
        Process childProcess = Process.Start(new ProcessStartInfo(sqlPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        }) ?? throw new Exception("Can't start the Language Server");

        childProcess.EnableRaisingEvents = true;
        childProcess.Exited += (sender, args) =>
        {
            if (disposedClients[connectionId.Id])
                return;
            
            if (childProcess.ExitCode != 0)
            {
                if (!lastCrashStats.TryGetValue(connectionId, out var crashStats) ||
                    crashStats.lastCrash + MaxCrashTimeWindow < DateTime.Now)
                {
                    lastCrashStats[connectionId] = (DateTime.Now, 1);
                }
                else
                {
                    lastCrashStats[connectionId] = (crashStats.lastCrash, crashStats.crashCount + 1);
                }
                
                if (lastCrashStats[connectionId].crashCount > MaxCrashCount)
                {
                    ForeachFile(connectionId, f => f.NotifyLanguageServerDied());
                    LOG.LogWarning("SQLS has crashed too many times in a short period of time. Stopping the server!");
                    return;
                }
                
                LOG.LogInformation("SQLS has crashed!!! Trying to restart the server!");
                RecreateClientAsync(connectionId).ListenErrors();
            }
        };

        var input = childProcess.StandardOutput.BaseStream;
        var output = childProcess.StandardInput.BaseStream;
        bool debugLanguageServer = false;

        if (debugLanguageServer)
        {
            input = new DebugStreamWrapper(input, Console.OpenStandardOutput());
            output = new DebugStreamWrapper(output, Console.OpenStandardOutput());
        }
        
        var client = await LanguageClient.From(o =>
            o.WithInput(input)
                .WithOutput(output)
                .OnLogMessage(x => Console.WriteLine((string?)x.Message))
                .OnLogTrace(x => Console.WriteLine(x.Message + " " + x.Verbose)));

        if (disposedClients[connectionId.Id])
        {
            childProcess.Kill();
            childProcess.Dispose();
            return client;
        }
        
        var credentials = this.credentials[connectionId.Id];
        client.DidChangeConfiguration(new DidChangeConfigurationParams()
        {
            Settings = new JObject()
            {
                new JProperty("sqls", new JObject()
                {
                    new JProperty("connections", new JArray(new JObject()
                    {
                        new JProperty("alias", "db"),
                        new JProperty("driver", "mysql"),
                        new JProperty("proto", "tcp"),
                        new JProperty("user", credentials.User),
                        new JProperty("passwd", credentials.Passwd),
                        new JProperty("host", credentials.Host),
                        new JProperty("port", credentials.Port),
                        new JProperty("dbName", credentials.SchemaName),
                        new JProperty("params", new JObject()
                        {
                            new JProperty("autocommit", "false")
                        })
                    }))
                })
            }
        });
        
        clients[connectionId.Id] = client;
        processes[connectionId.Id] = childProcess;
        return client;
    }

    public async Task<LanguageServerConnectionId> ConnectAsync(Guid connectionGuid, DatabaseCredentials credentials)
    {
        var hasId = guidToConnId.TryGetValue(connectionGuid, out var id);
        
        if (hasId)
        {
            if (connectionTasks.TryGetValue(id, out var pendingTask))
                await pendingTask;
            return id;
        }
        
        var taskCompletionSource = new TaskCompletionSource();
        guidToConnId[connectionGuid] = id = new LanguageServerConnectionId(clients.Count);
        connectionTasks[id] = taskCompletionSource.Task;
        this.credentials.Add(credentials);
        clients.Add(null!);
        processes.Add(null!);
        guids.Add(connectionGuid);
        disposedClients.Add(false);
        filesPerClient.Add(new List<SqlsLanguageServerFile>());

        await CreateClientAsync(id);
        connectionTasks.Remove(id);
        taskCompletionSource.SetResult();
        
        return id;
    }

    public async Task<ISqlLanguageServerFile> NewFileAsync(LanguageServerConnectionId connectionId)
    {
        if (connectionId.Id < 0 || connectionId.Id >= clients.Count)
            throw new Exception("Invalid connection id");

        if (clients[connectionId.Id] == null)
            throw new Exception("Language server failed to start");
        
        var file = new SqlsLanguageServerFile(this, connectionId, clients[connectionId.Id], fileId++);
        filesPerClient[connectionId.Id].Add(file);
        return file;
    }
    
    internal void UnregisterFile(SqlsLanguageServerFile file)
    {
        filesPerClient[file.ConnectionId.Id].Remove(file);
        if (filesPerClient[file.ConnectionId.Id].Count == 0)
        {
            clients[file.ConnectionId.Id] = null!;
            disposedClients[file.ConnectionId.Id] = true;
            if (processes[file.ConnectionId.Id] is { } p)
            {
                p.Kill();
                p.Dispose();
                processes[file.ConnectionId.Id] = null!;   
                guidToConnId.Remove(guids[file.ConnectionId.Id]);
            }
        }
    }
    
    internal void KillProcess(LanguageServerConnectionId connectionId)
    {
        processes[connectionId.Id]?.Kill();
    }

    public async Task RestartLanguageServerAsync(LanguageServerConnectionId connectionId)
    {
        if (processes[connectionId.Id]?.HasExited ?? false)
        {
            await RecreateClientAsync(connectionId);
        }
        else
            processes[connectionId.Id]?.Kill();
    }
}