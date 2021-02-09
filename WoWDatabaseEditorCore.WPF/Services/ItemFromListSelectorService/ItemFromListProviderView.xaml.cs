using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WoWDatabaseEditorCore.Services.ItemFromListSelectorService;

namespace WoWDatabaseEditorCore.WPF.Services.ItemFromListSelectorService
{
    /// <summary>
    ///     Interaction logic for ItemFromListProviderView.xaml
    /// </summary>
    public partial class ItemFromListProviderView : UserControl
    {
        public ItemFromListProviderView()
        {
            InitializeComponent();
            
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ScrollToSelectedItem();
            SearchBox.SelectionStart = 0;
            SearchBox.SelectionLength = SearchBox.Text.Length;
        }

        private void ScrollToSelectedItem()
        {
         /*   if (ListView.Items.Count == 0 || ListView.SelectedIndex < 0)
                return;
            
            VirtualizingStackPanel? vsp =  
                (VirtualizingStackPanel?)typeof(ItemsControl).InvokeMember("_itemsHost",
                    BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic, null, 
                    ListView, null);

            double scrollHeight = vsp!.ScrollOwner.ScrollableHeight;

            double offset = scrollHeight * ListView.SelectedIndex * 1.0f / ListView.Items.Count;

            vsp.SetVerticalOffset(offset);*/
        }

        private void ListView_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            //if (ListView.SelectedItem != null)
            //    ((ItemFromListProviderViewModel)DataContext).Accept.Execute(null);
        }
    }
}