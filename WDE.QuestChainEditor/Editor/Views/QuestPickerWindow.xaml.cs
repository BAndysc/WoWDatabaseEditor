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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WDE.QuestChainEditor.Editor.ViewModels;

namespace WDE.QuestChainEditor.Editor.Views
{
    /// <summary>
    /// Interaction logic for QuestPickerWindow.xaml
    /// </summary>
    public partial class QuestPickerWindow : Window
    {
        public QuestPickerWindow()
        {
            InitializeComponent();
        }
        
        private void NodePicker_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                DialogResult = true;
                Close();
            }
            else
                base.OnPreviewKeyDown(e);
        }
    }
}
