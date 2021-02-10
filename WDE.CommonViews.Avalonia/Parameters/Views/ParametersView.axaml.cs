using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.Parameters.Views
{
    /// <summary>
    ///     Interaction logic for ParametersView
    /// </summary>
    public class ParametersView : UserControl
    {
        public ParametersView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}