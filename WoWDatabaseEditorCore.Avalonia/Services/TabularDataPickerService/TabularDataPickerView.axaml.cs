using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaStyles.Controls;
using Prism.Commands;
using WDE.Common.Avalonia;

namespace WoWDatabaseEditorCore.Avalonia.Services.TabularDataPickerService;

public partial class TabularDataPickerView : UserControl
{
    public TabularDataPickerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        KeyBindings.Add(new KeyBinding()
        {
            Command = new DelegateCommand(() =>
            {
                this.GetControl<TextBox>("SearchBox").Focus();
            }),
            Gesture = new KeyGesture(Key.F, KeyGestures.CommandModifier)
        });
    }

    // quality of life feature: arrow down in searchbox focuses first element
    private void SearchBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            VirtualizedGridView gridView = this.GetControl<VirtualizedGridView>("GridView");

            if (gridView.FocusedIndex == null || gridView.FocusedIndex == -1)
            {
                gridView.Selection.Clear();
                if (gridView.Items.Count > 0)
                {
                    gridView.FocusedIndex = 0;
                    gridView.Selection.Add(0);
                }
            }
            gridView.Focus();

            e.Handled = true;
        }
    }
    
    private void GridView_OnKeyDown(object? sender, KeyEventArgs e)
    {
        // add missing functionality - space on the item (un)checks the checkbox
        if (e.Key == Key.Space && 
            sender is VirtualizedGridView gridView &&
            gridView.UseCheckBoxes &&
            gridView.FocusedIndex is { } focusedIndex)
        {
            if (gridView.CheckedIndices.Contains(focusedIndex))
                gridView.CheckedIndices.Remove(focusedIndex);
            else
                gridView.CheckedIndices.Add(focusedIndex);
            e.Handled = true;
        }
    }

    private void GridView_OnColumnWidthChanged(object? sender, RoutedEventArgs e)
    {
        var gv = this.FindControl<VirtualizedGridView>("GridView");
        if (gv is { } && DataContext is TabularDataPickerViewModel vm)
            vm.SaveColumnsWidth(gv.GetColumnsWidth().ToList());
    }
}
