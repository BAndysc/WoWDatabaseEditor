using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Mcp.Settings;

[UniqueProvider]
public interface IMcpSettingsProvider
{
    McpServerSettings Current { get; }
    void Update(McpServerSettings settings);
}

[AutoRegister]
[SingleInstance]
public class McpSettingsProvider : IMcpSettingsProvider
{
    private readonly IUserSettings userSettings;
    private McpServerSettings current;

    public McpSettingsProvider(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        current = userSettings.Get<McpServerSettings>(new McpServerSettings()) ?? new McpServerSettings();
    }

    public McpServerSettings Current => current;

    public void Update(McpServerSettings settings)
    {
        current = settings;
        userSettings.Update(settings);
    }
}
