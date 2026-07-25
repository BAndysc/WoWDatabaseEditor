using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.Mcp;
using WDE.Common.Sessions;
using WDE.Mcp.Services;
using WDE.Module.Attributes;

namespace WDE.Mcp.Tools;

public sealed class EditorInfoOutput
{
    [Description("The emulator core this editor instance is configured for; determines which script systems exist (SmartScript = Trinity-family, EventAI + dbscripts = CMaNGOS-family)")]
    public required string CoreVersion { get; init; }
    public required string CoreTag { get; init; }
    public required bool WorldDatabaseConnected { get; init; }
    public required bool RemoteServerConfigured { get; init; }
    public required bool RemoteServerConnected { get; init; }
    public required int OpenDocuments { get; init; }
    public required bool SessionActive { get; init; }
    [Description("Alphabetical list of all MCP tools available in this instance")]
    public required List<string> AvailableTools { get; init; }
}

[AutoRegister]
[SingleInstance]
public class EditorInfoTool : McpTool<EmptyInput, EditorInfoOutput>
{
    private readonly ICurrentCoreVersion currentCoreVersion;
    private readonly IMySqlExecutor worldExecutor;
    private readonly IRemoteConnectorService remoteConnector;
    private readonly IDocumentManager documentManager;
    private readonly ISessionService sessionService;
    private readonly Lazy<IMcpToolRegistry> registry;

    public EditorInfoTool(ICurrentCoreVersion currentCoreVersion,
        IMySqlExecutor worldExecutor,
        IRemoteConnectorService remoteConnector,
        IDocumentManager documentManager,
        ISessionService sessionService,
        Lazy<IMcpToolRegistry> registry)
    {
        this.currentCoreVersion = currentCoreVersion;
        this.worldExecutor = worldExecutor;
        this.remoteConnector = remoteConnector;
        this.documentManager = documentManager;
        this.sessionService = sessionService;
        this.registry = registry;
    }

    public override string Name => "editor_info";
    public override string Description => "START HERE: reports the editor's configuration (emulator core, database/server connectivity, open documents) and lists all available tools. Call this first to know what you can do in this instance.";

    protected override Task<EditorInfoOutput> Execute(EmptyInput input, CancellationToken token)
    {
        return Task.FromResult(new EditorInfoOutput
        {
            CoreVersion = currentCoreVersion.Current.FriendlyName,
            CoreTag = currentCoreVersion.Current.Tag,
            WorldDatabaseConnected = worldExecutor.IsConnected,
            RemoteServerConfigured = remoteConnector.HasValidSettings,
            RemoteServerConnected = remoteConnector.IsConnected,
            OpenDocuments = documentManager.OpenedDocuments.Count,
            SessionActive = sessionService.IsOpened,
            AvailableTools = registry.Value.ListTools().Select(t => t.Name).OrderBy(x => x).ToList()
        });
    }
}
