using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WDE.SmartScriptEditor.WPF.Editor.Views
{
    /// <summary>
    /// Interaction logic for SmartDataEventsEditorView.xaml
    /// </summary>
    public partial class SmartDataEventsEditorView : UserControl
    {
        public SmartDataEventsEditorView()
        {
            InitializeComponent();
        }
        
        private void actionIdInput_Error(object sender, System.Windows.Controls.ValidationErrorEventArgs e)
        {
            saveButton.IsEnabled = e.Action != System.Windows.Controls.ValidationErrorEventAction.Added;
        }
    }
}
