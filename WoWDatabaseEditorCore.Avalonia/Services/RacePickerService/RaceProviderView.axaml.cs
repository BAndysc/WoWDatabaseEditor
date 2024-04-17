using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using Prism.Commands;
using WDE.Common.Avalonia;

namespace WoWDatabaseEditorCore.Avalonia.Services.RacePickerService;

public partial class RaceProviderView : UserControl
{
    public RaceProviderView()
    {
        InitializeComponent();
        KeyBindings.Add(new KeyBinding()
        {
            Command = new DelegateCommand(() =>
            {
                this.GetControl<TextBox>("SearchBox").Focus();
            }),
            Gesture = new KeyGesture(Key.F, KeyGestures.CommandModifier)
        });
    }

    private void SearchBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            var index = ListBox.SelectedIndex;
            if (index < 0 || index >= ListBox.ItemCount)
                index = 0;
            ListBox.ContainerFromIndex(index)!.Focus(NavigationMethod.Tab);
            ListBox.Focus();
            e.Handled = true;
        }
    }

    private void GridView_OnKeyDown(object? sender, KeyEventArgs e)
    {
        // add missing functionality - space on the item (un)checks the checkbox
        if (e.Key == Key.Space &&
            sender is ListBox listBox)
        {
            if (ListBox.SelectedIndex >= 0 && ListBox.SelectedIndex < ListBox.ItemCount)
            {
                var checkBox = ListBox.ContainerFromIndex(ListBox.SelectedIndex)
                    .FindDescendantOfType<CheckBox>();
                if (checkBox != null)
                    checkBox.IsChecked = !checkBox.IsChecked;
            }
            e.Handled = true;
        }
    }
}