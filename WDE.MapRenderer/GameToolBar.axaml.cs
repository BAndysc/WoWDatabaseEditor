using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.MapRenderer
{
    public partial class GameToolBar : UserControl
    {
        public GameToolBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}