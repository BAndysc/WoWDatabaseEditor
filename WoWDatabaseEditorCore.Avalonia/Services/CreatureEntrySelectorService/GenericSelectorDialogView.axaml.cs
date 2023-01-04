using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Services.CreatureEntrySelectorService
{
    /// <summary>
    ///     Interaction logic for GenericSelectorDialogView.xaml
    /// </summary>
    public partial class GenericSelectorDialogView : DialogViewBase
    {
        public GenericSelectorDialogView()
        {
            InitializeComponent();
        }
        
        // quality of life feature: arrow down in searchbox focuses first element
        private void SearchBox_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                GridView gridView = this.GetControl<GridView>("GridView");

                if (gridView.ListBoxImpl != null)
                {
                    if (gridView.ListBoxImpl.SelectedItem == null)
                        gridView.ListBoxImpl.SelectedIndex = 0;
                    gridView.ListBoxImpl.ContainerFromIndex(gridView.ListBoxImpl.SelectedIndex)?.Focus();
                }
            }
        }
    }
}