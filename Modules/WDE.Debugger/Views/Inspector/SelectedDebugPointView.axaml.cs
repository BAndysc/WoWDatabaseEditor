using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.Debugger.Views.Inspector;

internal partial class SelectedDebugPointView : UserControl
{
    public SelectedDebugPointView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}