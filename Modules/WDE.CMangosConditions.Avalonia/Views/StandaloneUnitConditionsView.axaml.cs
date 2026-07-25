using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CMangosConditions.Avalonia.Views
{
    public partial class StandaloneUnitConditionsView : UserControl
    {
        public StandaloneUnitConditionsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
