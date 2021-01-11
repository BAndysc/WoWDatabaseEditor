using Ookii.Dialogs.Wpf;
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

namespace WDE.DbcStore.Views
{
    /// <summary>
    /// Interaction logic for DBCConfigView.xaml
    /// </summary>
    public partial class DBCConfigView : UserControl
    {
        public DBCConfigView()
        {
            InitializeComponent();
        }

        private void ShowFolderBrowse(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.SelectedPath = Path.Text;
            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Path.Text = dialog.SelectedPath;
            }
        }
    }
}
