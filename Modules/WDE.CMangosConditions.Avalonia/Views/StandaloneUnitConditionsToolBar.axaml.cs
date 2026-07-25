using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CMangosConditions.Avalonia.Views
{
    public partial class StandaloneUnitConditionsToolBar : UserControl
    {
        public StandaloneUnitConditionsToolBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
