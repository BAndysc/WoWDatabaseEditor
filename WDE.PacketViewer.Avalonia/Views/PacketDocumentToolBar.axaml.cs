using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.PacketViewer.Avalonia.Views
{
    public class PacketDocumentToolBar : UserControl
    {
        public PacketDocumentToolBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}