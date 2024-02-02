using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.Debugger.Views.Logs;

internal partial class DebuggerLogsToolView : UserControl
{
    public DebuggerLogsToolView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}