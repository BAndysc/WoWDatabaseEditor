using System.Windows;
using System.Windows.Input;

namespace WoWDatabaseEditor.Services.ItemFromListSelectorService
{
    /// <summary>
    /// Interaction logic for ItemFromListProviderView.xaml
    /// </summary>
    public partial class ItemFromListProviderView : Window
    {
        public ItemFromListProviderView()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
