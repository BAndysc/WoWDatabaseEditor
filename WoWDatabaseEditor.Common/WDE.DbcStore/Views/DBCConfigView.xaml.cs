using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace WDE.DbcStore.Views
{
    /// <summary>
    ///     Interaction logic for DBCConfigView.xaml
    /// </summary>
    public partial class DBCConfigView : UserControl
    {
        public DBCConfigView()
        {
            InitializeComponent();
        }

        private void ShowFolderBrowse(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new();
            dialog.SelectedPath = Path.Text;
            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
                Path.Text = dialog.SelectedPath;
        }
    }
}