using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CMangosConditions.Avalonia.Views
{
    public partial class MangosConditionsTreeEditor : UserControl
    {
        public MangosConditionsTreeEditor()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
