using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SqlWorkbench.Views;

public partial class SqlWorkbenchToolBar : UserControl
{
    public SqlWorkbenchToolBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}