using WDE.Common.Services;

namespace WDE.Mcp.Settings;

/// <summary>
/// Persisted configuration for the exposed MCP HTTP server.
/// Stored via <see cref="IUserSettings"/>.
/// </summary>
public class McpServerSettings : ISettings
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 3900;
}
