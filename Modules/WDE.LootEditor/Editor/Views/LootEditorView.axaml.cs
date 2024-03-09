using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Utils;
using WDE.LootEditor.Editor.ViewModels;

namespace WDE.LootEditor.Editor.Views;

public partial class LootEditorView : UserControl
{
    public LootEditorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void VeryFastTableView_OnValueUpdateRequest(string text)
    {
        (DataContext as LootEditorViewModel)!.UpdateSelectedCells(text).ListenErrors();
    }

    private int lastSortByColumnIndex = -1;
    private bool lastAscending = true;

    private void VeryFastTableView_OnColumnPressed(object? sender, ColumnPressedEventArgs e)
    {
        if (DataContext is LootEditorViewModel vm)
        {
            if (lastSortByColumnIndex != e.ColumnIndex)
            {
                lastSortByColumnIndex = e.ColumnIndex;
                lastAscending = true;
            }
            else
            {
                lastAscending = !lastAscending;
            }
            vm.SortElements(e.ColumnIndex, lastAscending);
        }
    }
}