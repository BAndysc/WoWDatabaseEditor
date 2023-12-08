using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CrashReport;

public partial class NoCrashView : UserControl
{
    public NoCrashView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}