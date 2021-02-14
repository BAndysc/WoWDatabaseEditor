using System.Windows.Controls;

namespace WDE.Conditions.Views
{
    public partial class ConditionEditorView : UserControl
    {
        public ConditionEditorView()
        {
            InitializeComponent();
        }
        
        private void idInput_ValidationError(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        {
            saveButton.IsEnabled = e.Action != System.Windows.Controls.ValidationErrorEventAction.Added;
        }
    }
}