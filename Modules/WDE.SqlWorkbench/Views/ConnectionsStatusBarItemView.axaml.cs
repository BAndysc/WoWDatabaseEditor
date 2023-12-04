using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SqlWorkbench.Views;

public partial class ConnectionsStatusBarItemView : UserControl
{
    public ConnectionsStatusBarItemView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}