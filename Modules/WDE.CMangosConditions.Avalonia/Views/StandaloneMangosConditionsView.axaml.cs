using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CMangosConditions.Avalonia.Views
{
    public partial class StandaloneMangosConditionsView : UserControl
    {
        public StandaloneMangosConditionsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
