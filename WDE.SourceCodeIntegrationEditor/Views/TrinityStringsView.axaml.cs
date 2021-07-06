using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SourceCodeIntegrationEditor.Views
{
    public class TrinityStringsView : UserControl
    {
        public TrinityStringsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}