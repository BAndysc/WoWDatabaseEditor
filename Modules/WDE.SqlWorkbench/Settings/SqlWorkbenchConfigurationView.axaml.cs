using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SqlWorkbench.Settings;

public partial class SqlWorkbenchConfigurationView : UserControl
{
    public SqlWorkbenchConfigurationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}