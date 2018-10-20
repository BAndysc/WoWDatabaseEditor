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
using WDE.Blueprints.Editor.ViewModels;

namespace WDE.Blueprints.Editor.Views
{
    /// <summary>
    /// Interaction logic for NodePickerWindow.xaml
    /// </summary>
    public partial class NodePickerWindow : Window
    {
        public NodePickerWindow()
        {
            InitializeComponent();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Close();
        }
    }
}
