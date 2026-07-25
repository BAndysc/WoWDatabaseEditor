using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Common.Services.Mcp;
using WDE.Module.Attributes;

namespace WDE.Mcp.Tools.Remote;

public sealed class RemoteStatusOutput
{
    public required bool IsConnected { get; init; }
    public required bool HasValidSettings { get; init; }
}

[AutoRegister]
[SingleInstance]
public class RemoteStatusTool : McpTool<EmptyInput, RemoteStatusOutput>
{
    private readonly IRemoteConnectorService remoteConnectorService;

    public RemoteStatusTool(IRemoteConnectorService remoteConnectorService)
    {
        this.remoteConnectorService = remoteConnectorService;
    }

    public override string Name => "remote_status";
    public override string Description => "Reports whether the editor is connected to a running game server (SOAP remote connection).";

    protected override Task<RemoteStatusOutput> Execute(EmptyInput input, CancellationToken token)
    {
        return Task.FromResult(new RemoteStatusOutput
        {
            IsConnected = remoteConnectorService.IsConnected,
            HasValidSettings = remoteConnectorService.HasValidSettings
        });
    }
}

public sealed class RemoteCommandInput
{
    [Description("Server console command to execute, i.e. 'server info' or 'reload creature_template'")]
    public required string Command { get; init; }
}

[AutoRegister]
[SingleInstance]
public class RemoteCommandTool : McpTool<RemoteCommandInput, string>
{
    private readonly IRemoteConnectorService remoteConnectorService;

    public RemoteCommandTool(IRemoteConnectorService remoteConnectorService)
    {
        this.remoteConnectorService = remoteConnectorService;
    }

    public override string Name => "remote_command";
    public override string Description => "DESTRUCTIVE: executes a console command on the connected game server via SOAP (i.e. reload commands) and returns the server's reply.";
    public override bool Mutating => true;
    public override McpToolThreadMode ThreadMode => McpToolThreadMode.Background;

    protected override async Task<string> Execute(RemoteCommandInput input, CancellationToken token)
    {
        if (!remoteConnectorService.HasValidSettings)
            throw new McpToolException("Remote connection is not configured in the editor settings");

        try
        {
            var response = await remoteConnectorService.ExecuteCommand(new AnonymousRemoteCommand(input.Command));
            return string.IsNullOrWhiteSpace(response) ? "(empty response)" : response;
        }
        catch (CouldNotConnectToRemoteServer e)
        {
            throw new McpToolException($"Could not connect to the remote server: {e.InnerException?.Message ?? e.Message}");
        }
        catch (RemoteConnectorException e)
        {
            throw new McpToolException($"Remote command failed: {e.Message}");
        }
    }
}
