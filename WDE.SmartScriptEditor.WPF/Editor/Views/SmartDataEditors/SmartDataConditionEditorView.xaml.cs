using System.Windows.Controls;

namespace WDE.SmartScriptEditor.WPF.Editor.Views
{
    public partial class SmartDataConditionEditorView : UserControl
    {
        public SmartDataConditionEditorView()
        {
            InitializeComponent();
        }
        
        private void actionIdInput_Error(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        {
            saveButton.IsEnabled = e.Action != System.Windows.Controls.ValidationErrorEventAction.Added;
        }
    }
}