using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.SmartScriptEditor.Editor.Views
{
    /// <summary>
    /// Interaction logic for SmartDataActionsEditorView.xaml
    /// </summary>
    public partial class SmartDataActionsEditorView : UserControl
    {
        public SmartDataActionsEditorView()
        {
            InitializeComponent();
        }

        private void actionIdInput_Error(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        {
            saveButton.IsEnabled = e.Action != System.Windows.Controls.ValidationErrorEventAction.Added;
        }
    }
}
