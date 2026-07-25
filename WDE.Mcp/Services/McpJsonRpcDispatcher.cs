using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace WDE.Mcp.Services;

/// <summary>
/// Minimal, stateless MCP JSON-RPC dispatcher. Handles the request/response subset
/// of the protocol needed for a tools-only server (no streaming, no sessions).
/// </summary>
public class McpJsonRpcDispatcher
{
    private static readonly string[] SupportedProtocolVersions = { "2024-11-05", "2025-03-26", "2025-06-18" };
    private static readonly string LatestProtocolVersion = SupportedProtocolVersions[^1];

    private readonly IMcpToolRegistry registry;

    public McpJsonRpcDispatcher(IMcpToolRegistry registry)
    {
        this.registry = registry;
    }

    /// <summary>Returns the response message, or null when no response shall be sent (notifications).</summary>
    public async Task<JsonRpcMessage?> Handle(JsonRpcMessage message, CancellationToken token)
    {
        if (message is JsonRpcNotification)
            return null;

        if (message is not JsonRpcRequest request)
            return null;

        try
        {
            return request.Method switch
            {
                "initialize" => Response(request, HandleInitialize(request)),
                "ping" => Response(request, new JsonObject()),
                "tools/list" => Response(request, new ListToolsResult { Tools = registry.ListTools().ToList() }),
                "tools/call" => Response(request, await HandleToolCall(request, token)),
                _ => Error(request, McpErrorCode.MethodNotFound, $"Method not supported: {request.Method}")
            };
        }
        catch (JsonException e)
        {
            return Error(request, McpErrorCode.InvalidParams, e.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[MCP] Internal error handling {request.Method}: {e}");
            return Error(request, McpErrorCode.InternalError, e.Message);
        }
    }

    private InitializeResult HandleInitialize(JsonRpcRequest request)
    {
        var initializeParams = Deserialize<InitializeRequestParams>(request.Params);
        var requestedVersion = initializeParams?.ProtocolVersion;
        var version = requestedVersion != null && SupportedProtocolVersions.Contains(requestedVersion)
            ? requestedVersion
            : LatestProtocolVersion;

        return new InitializeResult
        {
            ProtocolVersion = version,
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability()
            },
            ServerInfo = new Implementation
            {
                Name = "WoW Database Editor",
                Version = typeof(McpJsonRpcDispatcher).Assembly.GetName().Version?.ToString() ?? "1.0.0"
            },
            Instructions =
                "In-process MCP server of a running WoW Database Editor instance. Tools operate on the LIVE editor: " +
                "its connected world database, open documents and script editors. Call editor_info first to learn the " +
                "configured emulator core and which tools exist here (core determines the script system: SmartScript on " +
                "Trinity-family cores; EventAI + dbscripts + mangos conditions on CMaNGOS-family cores).\n\n" +
                "Conventions:\n" +
                "- OPEN DOCUMENTS FIRST: get/update tools automatically operate on a document open in the editor when there " +
                "is one (returned as fromOpenDocument/appliedToOpenDocument). Updates applied to an open document are visible " +
                "to the user and undoable; they are saved only when the user asks - then call document_save. documents_list " +
                "gives the index used by all document_* tools (problems, generate_sql, save, activate, close, undo, redo).\n" +
                "- REVIEW BEFORE EXECUTE: mutating tools default to execute=false and return the SQL they would run; only " +
                "pass execute=true when the user explicitly wants direct database changes.\n" +
                "- VALUE LOOKUPS: numeric ids (spells, creatures, factions, flags...) are described by named parameters - " +
                "use parameter_search (regex by name) and parameter_value_name to translate between ids and names; " +
                "table_describe tells which parameter key each column uses.\n" +
                "- Tables: table_list/table_describe/table_select expose the schema with friendly names; find_anywhere finds " +
                "all usages of a value across scripts, tables, spawns and conditions.\n\n" +
                "Tool families (availability depends on the core - check editor_info): smart_* (SmartScript AI scripts), " +
                "eventai_* (EventAI), dbscript_* (dbscripts_on_* timelines), trinity_condition_* / mangos_condition_* / " +
                "mangos_unit_condition_* (condition systems), packet_* (sniff analysis: open packet viewer documents, set " +
                "filters, run dumpers - packet_dump with the story teller dumper narrates a sniff), game_view_* / camera_* / " +
                "spawn_* (the 3D world view: fly the camera, inspect and edit spawns), documents_* / document_* (any open " +
                "editor tab), remote_* (commands on the live game server). Script *_data_search / dbscript_commands / " +
                "*_condition_types tools are the vocabularies - consult them before writing or editing a script/condition."
        };
    }

    private async Task<CallToolResult> HandleToolCall(JsonRpcRequest request, CancellationToken token)
    {
        var callParams = Deserialize<CallToolRequestParams>(request.Params)
                         ?? throw new JsonException("Missing tools/call params");
        return await registry.CallTool(callParams.Name, callParams.Arguments, token);
    }

    private static T? Deserialize<T>(JsonNode? node) where T : class
        => node == null ? null : JsonSerializer.Deserialize<T>(node, McpJsonUtilities.DefaultOptions);

    private static JsonRpcResponse Response<T>(JsonRpcRequest request, T result)
    {
        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = JsonSerializer.SerializeToNode(result, McpJsonUtilities.DefaultOptions)!
        };
    }

    private static JsonRpcError Error(JsonRpcRequest request, McpErrorCode code, string message)
    {
        return new JsonRpcError
        {
            Id = request.Id,
            Error = new JsonRpcErrorDetail
            {
                Code = (int)code,
                Message = message
            }
        };
    }
}
