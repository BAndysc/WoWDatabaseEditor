using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.MangosEventAiEditor.Settings
{
    public partial class CMangosConfigurationView : UserControl
    {
        public CMangosConfigurationView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
