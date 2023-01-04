using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.PathPreviewTool.Views;

public partial class PathPreviewToolBar : UserControl
{
    public PathPreviewToolBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}