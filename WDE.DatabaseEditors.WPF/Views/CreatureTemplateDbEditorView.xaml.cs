using System.Windows.Controls;

namespace WDE.DatabaseEditors.WPF.Views
{
    public partial class CreatureTemplateDbEditorView : UserControl
    {
        public CreatureTemplateDbEditorView()
        {
            InitializeComponent();
        }
        
        private void fieldInput_Error(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        {
            // saveButton.IsEnabled = e.Action != System.Windows.Controls.ValidationErrorEventAction.Added;
        }
    }
}