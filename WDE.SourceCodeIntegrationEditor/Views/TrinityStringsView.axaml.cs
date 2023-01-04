using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.SourceCodeIntegrationEditor.Views
{
    public partial class TrinityStringsView : UserControl
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