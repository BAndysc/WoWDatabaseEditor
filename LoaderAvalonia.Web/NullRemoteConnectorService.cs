using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace LoaderAvalonia.Web;

[AutoRegister]
[SingleInstance]
public class NullRemoteConnectorService : IRemoteConnectorService
{
    public event Action<string, TimeSpan, RemoteCommandType>? OnLog;

    public bool IsConnected => false;

    public bool HasValidSettings => true;

    public async Task<string> ExecuteCommand(IRemoteCommand command)
    {
        return "";
    }

    public async Task ExecuteCommands(IList<IRemoteCommand> commands)
    {
    }

    public event Action<string>? EditorCommandReceived;
    public event Action? EditorConnected;
    public event Action? EditorDisconnected;

    public IList<IRemoteCommand> Merge(IList<IRemoteCommand> commands)
    {
        return commands;
    }
}