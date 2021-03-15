using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CommonViews.Avalonia.Updater.Views
{
    public class UpdaterConfigurationView : UserControl
    {
        public UpdaterConfigurationView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}