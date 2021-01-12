using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace WoWDatabaseEditor.Services.NewItemService
{
    /// <summary>
    ///     Interaction logic for NewItemWindow.xaml
    /// </summary>
    public partial class NewItemWindow : MetroWindow
    {
        public NewItemWindow(INewItemWindowViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}