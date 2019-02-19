using MahApps.Metro.Controls;
using System.Windows;

namespace WoWDatabaseEditor.Services.NewItemService
{
    /// <summary>
    /// Interaction logic for NewItemWindow.xaml
    /// </summary>
    public partial class NewItemWindow : MetroWindow
    {
        public NewItemWindow(INewItemWindowViewModel viewModel)
        {
            this.DataContext = viewModel;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
