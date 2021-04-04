using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
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

        private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
        {
            // add missing functionality - space on the item (un)checks the checkbox
            if (e.Key == Key.Space && sender is GridView gridView)
            {
                ListBox list = gridView.ListBoxImpl;
                if (list.SelectedItem == null)
                    return;

                var selected = list.ItemContainerGenerator.ContainerFromIndex(list.SelectedIndex);

                if (selected == null)
                    return;

                var checkBox = selected.FindDescendantOfType<CheckBox>();

                if (checkBox != null)
                    checkBox.IsChecked = !checkBox.IsChecked;
            }
        }

        // quality of life feature: arrow down in searchbox focuses first element
        private void SearchBox_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                GridView gridView = this.FindControl<GridView>("GridView");
                if (gridView == null)
                    return;

                if (gridView.ListBoxImpl.SelectedItem == null)
                    gridView.ListBoxImpl.SelectedIndex = 0;
                gridView.ListBoxImpl.ItemContainerGenerator?.ContainerFromIndex(gridView.ListBoxImpl.SelectedIndex)?.Focus();
            }
        }
    }
}