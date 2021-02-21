using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Services.CreatureEntrySelectorService
{
    /// <summary>
    ///     Interaction logic for GenericSelectorDialogView.xaml
    /// </summary>
    public class GenericSelectorDialogView : DialogViewBase
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