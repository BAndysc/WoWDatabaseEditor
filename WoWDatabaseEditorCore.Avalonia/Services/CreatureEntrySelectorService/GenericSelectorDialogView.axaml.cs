using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WoWDatabaseEditorCore.Avalonia.Services.CreatureEntrySelectorService
{
    /// <summary>
    ///     Interaction logic for GenericSelectorDialogView.xaml
    /// </summary>
    public partial class GenericSelectorDialogView : UserControl
    {
        public GenericSelectorDialogView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}