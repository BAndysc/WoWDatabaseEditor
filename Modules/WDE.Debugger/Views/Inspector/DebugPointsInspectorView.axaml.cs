using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.Debugger.Views.Inspector;

internal partial class DebugPointsInspectorView : UserControl
{
    public DebugPointsInspectorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}