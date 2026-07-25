using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CMangosConditions.Avalonia.Views
{
    public partial class StandaloneMangosConditionsToolBar : UserControl
    {
        public StandaloneMangosConditionsToolBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
