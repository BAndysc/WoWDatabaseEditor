using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SourceCodeIntegrationEditor.SourceCode;

public partial class SourceCodeConfigurationView : UserControl
{
    public SourceCodeConfigurationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}