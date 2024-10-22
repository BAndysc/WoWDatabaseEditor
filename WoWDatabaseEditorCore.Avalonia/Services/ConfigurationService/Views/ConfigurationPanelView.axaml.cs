using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using AvaloniaStyles;

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
            if (SystemTheme.EffectiveTheme == SystemThemeOptions.Windows9x)
            {
                Content = ((Template?)Resources["Win9xTemplate"])?.Build() ?? new TextBlock(){Text = "Could not load Win9x template"};
            }
            else
            {
                Content = ((Template?)Resources["DefaultTemplate"])?.Build() ?? new TextBlock(){Text = "Could not load default template"};
            }
        }
    }
}