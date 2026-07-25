using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.Mcp.Settings;

public partial class McpConfigurationView : UserControl
{
    public McpConfigurationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
