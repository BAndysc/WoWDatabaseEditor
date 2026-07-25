using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Services.PersonalGuidService;

public partial class PersonalGuidRangeConfigurationView : UserControl
{
    public PersonalGuidRangeConfigurationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
