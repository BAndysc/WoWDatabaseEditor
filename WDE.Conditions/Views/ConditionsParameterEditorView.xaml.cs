using System.Windows.Controls;

namespace WDE.Conditions.Views
{
    public partial class ConditionsParameterEditorView : UserControl
    {
        public ConditionsParameterEditorView()
        {
            InitializeComponent();
        }
        
        private void defaultValInput_Error(object sender, ValidationErrorEventArgs e)
        {
            saveButton.IsEnabled = e.Action != ValidationErrorEventAction.Added;
        }
    }
}