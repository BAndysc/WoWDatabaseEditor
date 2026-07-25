using System.Text.Json;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using ModelContextProtocol.Protocol;
using NUnit.Framework;
using WDE.Common.Services.Mcp;
using WDE.Common.Tasks;
using WDE.Mcp.Services;

namespace WDE.Mcp.Test;

public class FakeMainThread : IMainThread
{
    public int ScheduledCount;

    public IDisposable Delay(Action action, TimeSpan delay) => throw new NotImplementedException();
    public void Dispatch(Action action) => action();
    public Task Dispatch(Func<Task> action) => action();
    public IDisposable StartTimer(Func<bool> action, TimeSpan interval) => throw new NotImplementedException();

    public Task<T> Schedule<T>(Func<Task<T>> func)
    {
        ScheduledCount++;
        return func();
    }

    public Task<T> Schedule<T>(Func<T> func)
    {
        ScheduledCount++;
        return Task.FromResult(func());
    }
}

public enum TestKind
{
    Alpha,
    Beta
}

public sealed class TestInput
{
    [Description("some required number")]
    public required long Entry { get; init; }

    [Description("optional kind")]
    public TestKind Kind { get; init; } = TestKind.Alpha;

    public string? Comment { get; init; }
}

public sealed class TestOutput
{
    public required long Entry { get; init; }
    public required string Kind { get; init; }
}

public class TestTool : McpTool<TestInput, TestOutput>
{
    public override string Name => "test_tool";
    public override string Description => "test tool";

    protected override Task<TestOutput> Execute(TestInput input, CancellationToken token)
        => Task.FromResult(new TestOutput { Entry = input.Entry, Kind = input.Kind.ToString() });
}

public class BackgroundThrowingTool : McpTool<EmptyInput, string>
{
    public override string Name => "throwing_tool";
    public override string Description => "always throws";
    public override McpToolThreadMode ThreadMode => McpToolThreadMode.Background;

    protected override Task<string> Execute(EmptyInput input, CancellationToken token)
        => throw new McpToolException("clean error");
}

public class DuplicateNameTool : McpTool<EmptyInput, string>
{
    public override string Name => "test_tool";
    public override string Description => "duplicate";

    protected override Task<string> Execute(EmptyInput input, CancellationToken token)
        => Task.FromResult("dup");
}

[TestFixture]
public class McpToolRegistryTests
{
    private FakeMainThread mainThread = null!;
    private McpToolRegistry registry = null!;

    [SetUp]
    public void Setup()
    {
        mainThread = new FakeMainThread();
        registry = new McpToolRegistry(new IMcpTool[] { new TestTool(), new BackgroundThrowingTool(), new DuplicateNameTool() }, mainThread);
    }

    private static string Text(CallToolResult result) => ((TextContentBlock)result.Content![0]).Text;

    [Test]
    public void ListTools_GeneratesSchemaFromInputType()
    {
        var tools = registry.ListTools();
        Assert.That(tools.Select(t => t.Name), Is.EquivalentTo(new[] { "test_tool", "throwing_tool" }));

        var tool = tools.Single(t => t.Name == "test_tool");
        var schema = tool.InputSchema;
        Assert.That(schema.GetProperty("type").GetString(), Is.EqualTo("object"));

        var properties = schema.GetProperty("properties");
        Assert.That(properties.TryGetProperty("entry", out var entry), Is.True);
        Assert.That(entry.GetProperty("description").GetString(), Is.EqualTo("some required number"));
        Assert.That(properties.TryGetProperty("kind", out var kind), Is.True);
        Assert.That(kind.GetProperty("enum").EnumerateArray().Select(e => e.GetString()),
            Does.Contain("alpha").Or.Contain("Alpha"));

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.That(required, Does.Contain("entry"));
        Assert.That(required, Does.Not.Contain("comment"));
    }

    [Test]
    public void ListTools_SkipsDuplicateNames()
    {
        Assert.That(registry.ToolCount, Is.EqualTo(2));
    }

    [Test]
    public void ListTools_SetsReadOnlyHint()
    {
        var tool = registry.ListTools().Single(t => t.Name == "test_tool");
        Assert.That(tool.Annotations?.ReadOnlyHint, Is.True);
    }

    [Test]
    public async Task CallTool_BindsTypedInput_AndSerializesOutput()
    {
        var args = new Dictionary<string, JsonElement>
        {
            ["entry"] = JsonDocument.Parse("123").RootElement,
            ["kind"] = JsonDocument.Parse("\"beta\"").RootElement
        };
        var result = await registry.CallTool("test_tool", args, CancellationToken.None);

        Assert.That(result.IsError, Is.Null.Or.False);
        var output = JsonSerializer.Deserialize<JsonElement>(Text(result));
        Assert.That(output.GetProperty("entry").GetInt64(), Is.EqualTo(123));
        Assert.That(output.GetProperty("kind").GetString(), Is.EqualTo("Beta"));
    }

    [Test]
    public async Task CallTool_MissingRequiredArgument_ReturnsError()
    {
        var result = await registry.CallTool("test_tool", new Dictionary<string, JsonElement>(), CancellationToken.None);
        Assert.That(result.IsError, Is.True);
        Assert.That(Text(result), Does.Contain("Invalid arguments"));
    }

    [Test]
    public async Task CallTool_UnknownTool_ReturnsError()
    {
        var result = await registry.CallTool("nope", null, CancellationToken.None);
        Assert.That(result.IsError, Is.True);
        Assert.That(Text(result), Does.Contain("Unknown tool"));
    }

    [Test]
    public async Task CallTool_McpToolException_ReturnsCleanError()
    {
        var result = await registry.CallTool("throwing_tool", null, CancellationToken.None);
        Assert.That(result.IsError, Is.True);
        Assert.That(Text(result), Is.EqualTo("clean error"));
    }

    [Test]
    public async Task CallTool_MainThreadTool_IsScheduledOnMainThread()
    {
        var args = new Dictionary<string, JsonElement> { ["entry"] = JsonDocument.Parse("1").RootElement };
        await registry.CallTool("test_tool", args, CancellationToken.None);
        Assert.That(mainThread.ScheduledCount, Is.EqualTo(1));
    }

    [Test]
    public async Task CallTool_BackgroundTool_IsNotScheduledOnMainThread()
    {
        await registry.CallTool("throwing_tool", null, CancellationToken.None);
        Assert.That(mainThread.ScheduledCount, Is.EqualTo(0));
    }
}
