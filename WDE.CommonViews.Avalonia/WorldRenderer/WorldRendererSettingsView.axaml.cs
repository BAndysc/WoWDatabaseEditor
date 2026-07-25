using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CommonViews.Avalonia.WorldRenderer
{
    public partial class WorldRendererSettingsView : UserControl
    {
        public WorldRendererSettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
