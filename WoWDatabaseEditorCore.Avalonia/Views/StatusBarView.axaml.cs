using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    /// <summary>
    ///     Interaction logic for AboutView
    /// </summary>
    public class StatusBarView : UserControl
    {
        public StatusBarView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}