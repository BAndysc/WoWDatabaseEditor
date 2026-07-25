using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using NUnit.Framework;
using WDE.Common.Services.Mcp;
using WDE.Mcp.Services;

namespace WDE.Mcp.Test;

[TestFixture]
public class McpJsonRpcDispatcherTests
{
    private McpJsonRpcDispatcher dispatcher = null!;

    [SetUp]
    public void Setup()
    {
        var registry = new McpToolRegistry(new IMcpTool[] { new TestTool() }, new FakeMainThread());
        dispatcher = new McpJsonRpcDispatcher(registry);
    }

    private static JsonRpcRequest Request(string method, object? @params = null) => new()
    {
        Id = new RequestId(1),
        Method = method,
        Params = @params == null ? null : JsonSerializer.SerializeToNode(@params, McpJsonUtilities.DefaultOptions)
    };

    [Test]
    public async Task Initialize_NegotiatesKnownProtocolVersion()
    {
        var response = await dispatcher.Handle(Request("initialize", new InitializeRequestParams
        {
            ProtocolVersion = "2025-03-26",
            Capabilities = new ClientCapabilities(),
            ClientInfo = new Implementation { Name = "test", Version = "1.0" }
        }), CancellationToken.None);

        var result = JsonSerializer.Deserialize<InitializeResult>(((JsonRpcResponse)response!).Result, McpJsonUtilities.DefaultOptions)!;
        Assert.That(result.ProtocolVersion, Is.EqualTo("2025-03-26"));
        Assert.That(result.Capabilities.Tools, Is.Not.Null);
    }

    [Test]
    public async Task Initialize_UnknownVersion_FallsBackToLatest()
    {
        var response = await dispatcher.Handle(Request("initialize", new InitializeRequestParams
        {
            ProtocolVersion = "1999-01-01",
            Capabilities = new ClientCapabilities(),
            ClientInfo = new Implementation { Name = "test", Version = "1.0" }
        }), CancellationToken.None);

        var result = JsonSerializer.Deserialize<InitializeResult>(((JsonRpcResponse)response!).Result, McpJsonUtilities.DefaultOptions)!;
        Assert.That(result.ProtocolVersion, Is.EqualTo("2025-06-18"));
    }

    [Test]
    public async Task ToolsList_ReturnsTools()
    {
        var response = await dispatcher.Handle(Request("tools/list"), CancellationToken.None);
        var result = JsonSerializer.Deserialize<ListToolsResult>(((JsonRpcResponse)response!).Result, McpJsonUtilities.DefaultOptions)!;
        Assert.That(result.Tools!.Select(t => t.Name), Does.Contain("test_tool"));
    }

    [Test]
    public async Task ToolsCall_RunsTool()
    {
        var response = await dispatcher.Handle(Request("tools/call", new CallToolRequestParams
        {
            Name = "test_tool",
            Arguments = new Dictionary<string, JsonElement> { ["entry"] = JsonDocument.Parse("7").RootElement }
        }), CancellationToken.None);

        var result = JsonSerializer.Deserialize<CallToolResult>(((JsonRpcResponse)response!).Result, McpJsonUtilities.DefaultOptions)!;
        Assert.That(result.IsError, Is.Null.Or.False);
        Assert.That(((TextContentBlock)result.Content![0]).Text, Does.Contain("7"));
    }

    [Test]
    public async Task Ping_ReturnsEmptyResult()
    {
        var response = await dispatcher.Handle(Request("ping"), CancellationToken.None);
        Assert.That(response, Is.InstanceOf<JsonRpcResponse>());
    }

    [Test]
    public async Task UnknownMethod_ReturnsMethodNotFound()
    {
        var response = await dispatcher.Handle(Request("resources/list"), CancellationToken.None);
        Assert.That(response, Is.InstanceOf<JsonRpcError>());
        Assert.That(((JsonRpcError)response!).Error.Code, Is.EqualTo((int)McpErrorCode.MethodNotFound));
    }

    [Test]
    public async Task Notification_ReturnsNoResponse()
    {
        var response = await dispatcher.Handle(new JsonRpcNotification { Method = "notifications/initialized" }, CancellationToken.None);
        Assert.That(response, Is.Null);
    }
}
