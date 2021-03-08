using System.Windows.Controls;

namespace WDE.DatabaseEditors.WPF.Views
{
    public partial class TemplateDbTableEditorView : UserControl
    {
        public TemplateDbTableEditorView()
        {
            InitializeComponent();
        }
        
        private void fieldInput_Error(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        {
            // saveButton.IsEnabled = e.Action != System.Windows.Controls.ValidationErrorEventAction.Added;
        }
    }
}