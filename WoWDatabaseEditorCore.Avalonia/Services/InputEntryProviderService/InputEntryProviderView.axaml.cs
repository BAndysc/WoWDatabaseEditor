using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Services.InputEntryProviderService
{
    public class InputEntryProviderView : UserControl
    {
        public InputEntryProviderView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}