using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Services.ItemFromListSelectorService
{
    /// <summary>
    ///     Interaction logic for ItemFromListProviderView.xaml
    /// </summary>
    public partial class ItemFromListProviderView : UserControl
    {
        public ItemFromListProviderView()
        {
            InitializeComponent();
            
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}