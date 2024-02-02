using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.Debugger.Views.Inspector;

internal partial class DebugPointsInspectorToolBar : UserControl
{
    public DebugPointsInspectorToolBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}