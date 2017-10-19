using System.Windows;

namespace WoWDatabaseEditor.Services.CreatureEntrySelectorService
{
    /// <summary>
    /// Interaction logic for CreatureEntrySelectorWindow.xaml
    /// </summary>
    public partial class CreatureEntrySelectorWindow : Window
    {
        public CreatureEntrySelectorWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
