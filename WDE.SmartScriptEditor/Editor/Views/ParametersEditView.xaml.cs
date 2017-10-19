using System.Windows;

namespace WDE.SmartScriptEditor.Editor.Views
{
    /// <summary>
    /// Interaction logic for ParametersEditView.xaml
    /// </summary>
    public partial class ParametersEditView : Window
    {
        public ParametersEditView()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
