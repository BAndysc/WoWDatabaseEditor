using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.Common.Avalonia.Utils
{
    public partial class LoadingSpinner : UserControl
    {
        public LoadingSpinner()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}