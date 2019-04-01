using System.Windows;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.Conditions.Providers;


namespace WDE.SmartScriptEditor.Editor.Views
{
    /// <summary>
    /// Interaction logic for ParametersEditView.xaml
    /// </summary>
    public partial class ParametersEditView : Window
    {
        private readonly IConditionsEditViewProvider conditionsEditViewProvider;

        public ParametersEditView(IConditionsEditViewProvider conditionsEditViewProvider)
        {
            InitializeComponent();
            this.conditionsEditViewProvider = conditionsEditViewProvider;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void editConditionsButton_Click(object sender, RoutedEventArgs e)
        {
            ParametersEditViewModel model = DataContext as ParametersEditViewModel;

            if (model._conditions != null)
                conditionsEditViewProvider.OpenWindow(model._conditions);
        }
    }
}
