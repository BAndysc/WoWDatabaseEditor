using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.Common.Avalonia.Utils
{
    public class LoadingSpinner : UserControl
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