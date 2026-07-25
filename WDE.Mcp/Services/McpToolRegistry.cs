using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using WDE.Common.Services.Mcp;
using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WDE.Mcp.Services;

[UniqueProvider]
public interface IMcpToolRegistry
{
    IReadOnlyList<Tool> ListTools();
    Task<CallToolResult> CallTool(string name, IDictionary<string, JsonElement>? arguments, CancellationToken token);
    int ToolCount { get; }
}

[AutoRegister]
[SingleInstance]
public class McpToolRegistry : IMcpToolRegistry
{
    private readonly IMainThread mainThread;
    private readonly Lazy<Dictionary<string, (IMcpTool tool, Tool descriptor)>> tools;

    public McpToolRegistry(IEnumerable<IMcpTool> allTools, IMainThread mainThread)
    {
        this.mainThread = mainThread;
        tools = new Lazy<Dictionary<string, (IMcpTool, Tool)>>(() => BuildCatalog(allTools));
    }

    private static Dictionary<string, (IMcpTool, Tool)> BuildCatalog(IEnumerable<IMcpTool> allTools)
    {
        var catalog = new Dictionary<string, (IMcpTool, Tool)>();
        foreach (var tool in allTools)
        {
            try
            {
                if (catalog.ContainsKey(tool.Name))
                {
                    Console.WriteLine($"[MCP] Duplicate tool name '{tool.Name}' ({tool.GetType()}), skipping");
                    continue;
                }

                var schema = AIJsonUtilities.CreateJsonSchema(tool.InputType, serializerOptions: McpToolSerialization.Options);
                var descriptor = new Tool
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    InputSchema = schema,
                    Annotations = new ToolAnnotations
                    {
                        ReadOnlyHint = !tool.Mutating
                    }
                };
                catalog[tool.Name] = (tool, descriptor);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[MCP] Failed to register tool {tool.GetType()}: {e}");
            }
        }
        return catalog;
    }

    public int ToolCount => tools.Value.Count;

    public IReadOnlyList<Tool> ListTools() => tools.Value.Values.Select(x => x.descriptor).ToList();

    public async Task<CallToolResult> CallTool(string name, IDictionary<string, JsonElement>? arguments, CancellationToken token)
    {
        if (!tools.Value.TryGetValue(name, out var entry))
            return Error($"Unknown tool: {name}");

        object input;
        try
        {
            input = BindInput(entry.tool.InputType, arguments);
        }
        catch (JsonException e)
        {
            return Error($"Invalid arguments: {e.Message}");
        }

        try
        {
            var result = entry.tool.ThreadMode == McpToolThreadMode.Background
                ? await entry.tool.Execute(input, token)
                : await mainThread.Schedule(() => entry.tool.Execute(input, token));

            var text = result switch
            {
                null => "(no result)",
                string s => s,
                _ => JsonSerializer.Serialize(result, result.GetType(), McpToolSerialization.Options)
            };
            return new CallToolResult
            {
                Content = new List<ContentBlock> { new TextContentBlock { Text = text } }
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (McpToolException e)
        {
            return Error(e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[MCP] Tool '{name}' failed: {e}");
            return Error($"Tool failed: {e.Message}");
        }
    }

    private static object BindInput(Type inputType, IDictionary<string, JsonElement>? arguments)
    {
        var jsonObject = new JsonObject();
        if (arguments != null)
        {
            foreach (var (key, value) in arguments)
                jsonObject[key] = JsonNode.Parse(value.GetRawText());
        }

        return JsonSerializer.Deserialize(jsonObject, inputType, McpToolSerialization.Options)
               ?? throw new JsonException("Arguments deserialized to null");
    }

    private static CallToolResult Error(string message)
    {
        return new CallToolResult
        {
            IsError = true,
            Content = new List<ContentBlock> { new TextContentBlock { Text = message } }
        };
    }
}
