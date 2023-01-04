using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.PacketViewer.Avalonia.Views
{
    public partial class PacketViewerConfigurationView : UserControl
    {
        public PacketViewerConfigurationView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}