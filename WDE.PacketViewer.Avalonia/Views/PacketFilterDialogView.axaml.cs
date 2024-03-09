using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Avalonia.Views
{
    public partial class PacketFilterDialogView : DialogViewBase
    {
        public PacketFilterDialogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}