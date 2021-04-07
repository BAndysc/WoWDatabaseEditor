using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Views
{
    /// <summary>
    ///     Interaction logic for ThemeConfigView.xaml
    /// </summary>
    public partial class ThemeConfigView : UserControl
    {
        public ThemeConfigView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}