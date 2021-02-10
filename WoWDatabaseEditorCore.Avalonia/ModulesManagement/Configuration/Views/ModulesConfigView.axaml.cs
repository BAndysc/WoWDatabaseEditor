using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.ModulesManagement.Configuration.Views
{
    /// <summary>
    ///     Interaction logic for ModulesConfigView.xaml
    /// </summary>
    public class ModulesConfigView : UserControl
    {
        public ModulesConfigView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}