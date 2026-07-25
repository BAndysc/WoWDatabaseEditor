using System;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services.Mcp;

public enum McpToolThreadMode
{
    /// <summary>The tool touches view models, documents or other UI-affine state and must run on the main thread.</summary>
    MainThread,
    /// <summary>The tool is thread-safe (i.e. pure database access) and can run on a thread pool thread.</summary>
    Background
}

/// <summary>
/// A single tool exposed over the editor's MCP server. Implementations are discovered
/// via DI (register with [AutoRegister], or [AutoRegisterToParentScope] inside scoped modules).
/// The MCP host binds the incoming arguments JSON to <see cref="InputType"/>, generates the
/// input schema from that type ([Description] attributes and required properties are honored)
/// and serializes whatever object <see cref="Execute"/> returns back to the client.
/// </summary>
[NonUniqueProvider]
public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    /// <summary>True if the tool can modify editor or database state.</summary>
    bool Mutating { get; }
    McpToolThreadMode ThreadMode { get; }
    Type InputType { get; }
    /// <summary>Executes the tool. <paramref name="input"/> is an instance of <see cref="InputType"/>.</summary>
    Task<object?> Execute(object input, CancellationToken token);
}

/// <summary>
/// Typed base class for MCP tools, asp.net-handler style: input is model-bound by the host,
/// the returned output object is serialized by the host.
/// </summary>
public abstract class McpTool<TInput, TOutput> : IMcpTool where TInput : class
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual bool Mutating => false;
    public virtual McpToolThreadMode ThreadMode => McpToolThreadMode.MainThread;
    public Type InputType => typeof(TInput);

    protected abstract Task<TOutput> Execute(TInput input, CancellationToken token);

    async Task<object?> IMcpTool.Execute(object input, CancellationToken token)
        => await Execute((TInput)input, token);
}

/// <summary>Input type for tools that take no arguments.</summary>
public sealed class EmptyInput
{
    public static readonly EmptyInput Instance = new();
}

/// <summary>
/// Thrown by tools to report a clean, user-actionable error to the MCP client
/// (i.e. "unknown table", "database not connected") without a stack trace.
/// </summary>
public class McpToolException : Exception
{
    public McpToolException(string userMessage) : base(userMessage) { }
}
