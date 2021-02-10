using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    /// <summary>
    ///     Interaction logic for ModulesConfigView.xaml
    /// </summary>
    public class SplashScreenView : Window
    {
        public SplashScreenView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}