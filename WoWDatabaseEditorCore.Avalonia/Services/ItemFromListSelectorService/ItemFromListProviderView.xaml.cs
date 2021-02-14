using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Services.ItemFromListSelectorService
{
    /// <summary>
    ///     Interaction logic for ItemFromListProviderView.xaml
    /// </summary>
    public class ItemFromListProviderView : DialogViewBase
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