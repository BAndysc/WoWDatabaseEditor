using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Services.IdGeneratorService;

public partial class IdGeneratorConfigurationView : UserControl
{
    public IdGeneratorConfigurationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
