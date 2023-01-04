using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Services.ConfigurationService.Views
{
    /// <summary>
    ///     Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationPanelView : UserControl
    {
        public ConfigurationPanelView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}