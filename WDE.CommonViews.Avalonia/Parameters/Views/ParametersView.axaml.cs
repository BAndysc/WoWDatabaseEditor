using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CommonViews.Avalonia.Parameters.Views
{
    /// <summary>
    ///     Interaction logic for ParametersView
    /// </summary>
    public partial class ParametersView : UserControl
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