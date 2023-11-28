using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace WDE.SqlWorkbench.Views;

public partial class ActionsOutputView : UserControl
{
    public ActionsOutputView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void GridView_OnColumnWidthChanged(object? sender, RoutedEventArgs e)
    {
        
    }
}