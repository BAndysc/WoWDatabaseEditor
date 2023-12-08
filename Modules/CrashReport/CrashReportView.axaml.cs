using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CrashReport;

public partial class CrashReportView : UserControl
{
    public CrashReportView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}