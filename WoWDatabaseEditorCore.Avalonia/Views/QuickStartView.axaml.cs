using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public class QuickStartView : UserControl
    {
        public QuickStartView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}