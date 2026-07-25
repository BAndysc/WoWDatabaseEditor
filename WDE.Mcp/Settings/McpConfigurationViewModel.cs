using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Types;
using WDE.Mcp.Services;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.Mcp.Settings;

[AutoRegister]
public partial class McpConfigurationViewModel : ObservableBase, IConfigurable
{
    private readonly IMcpSettingsProvider settingsProvider;

    [Notify] private bool enabled;
    [Notify] private int port;
    [Notify] private bool isModified;

    public IMcpServerStatus Status { get; }

    public McpConfigurationViewModel(IMcpSettingsProvider settingsProvider, IMcpServerStatus status)
    {
        this.settingsProvider = settingsProvider;
        Status = status;

        enabled = settingsProvider.Current.Enabled;
        port = settingsProvider.Current.Port;

        Save = new DelegateCommand(() =>
        {
            settingsProvider.Update(new McpServerSettings
            {
                Enabled = Enabled,
                Port = Port
            });
            IsModified = false;
        });

        On(() => Enabled, _ => IsModified = true);
        On(() => Port, _ => IsModified = true);
        IsModified = false;
    }

    public ICommand Save { get; }
    public string Name => "MCP server";
    public ImageUri Icon { get; } = new ImageUri("Icons/document_remotedesktop_big.png");
    public string? ShortDescription => "Model Context Protocol server exposing the editor to AI clients (Claude Code and others) over HTTP. Tools cover the database, generic table editors, script editors and more.";
    public bool IsRestartRequired => true;
    public ConfigurableGroup Group => ConfigurableGroup.Advanced;
}
