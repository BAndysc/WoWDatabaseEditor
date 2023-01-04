using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.PacketViewer.Avalonia.Views
{
    public partial class PacketDocumentToolBar : UserControl
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