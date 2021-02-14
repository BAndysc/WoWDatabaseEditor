using System.Windows.Controls;

namespace WDE.Conditions.WPF.Views
{
    public partial class ConditionSourceEditorView : UserControl
    {
        public ConditionSourceEditorView()
        {
            InitializeComponent();
        }
        
        private void idInput_ValidationError(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        {
            saveButton.IsEnabled = e.Action != System.Windows.Controls.ValidationErrorEventAction.Added;
        }
    }
}