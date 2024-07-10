using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.Common.Avalonia;
using WDE.DatabaseEditors.Services.TablesPanel;

namespace WDE.DatabaseEditors.Avalonia.Services.TablesPanel;

public partial class FancyEditorsTablesToolGroupView : UserControl
{
    public FancyEditorsTablesToolGroupView()
    {
        InitializeComponent();
    }
    
    private ISelectionAdapter SelectionAdapter = null!;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SelectionAdapter = new SelectingItemsControlSelectionAdapter(TablesListBox);
        DispatcherTimer.RunOnce(() =>
        {
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            searchTextBox?.Focus();
            searchTextBox?.SelectAll();
        }, TimeSpan.FromMilliseconds(1));
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox itemsBox && e.Source is Visual visual)
        {
            if (visual.SelfOrVisualAncestor<ListBoxItem>() is { } item)
            {
                (DataContext as FancyEditorsTablesToolGroupViewModel)!.OpenTable((item.DataContext as TableItemViewModel)!);
            }
        }
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            if (sender is ListBox itemsBox && e.Source is Visual visual)
            {
                if (visual.SelfOrVisualAncestor<ListBoxItem>() is { } item)
                {
                    (DataContext as FancyEditorsTablesToolGroupViewModel)!.OpenTable((item.DataContext as TableItemViewModel)!);
                    e.Handled = true;
                }
            }
        }
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is ListBox itemsBox && e.Source is Visual visual)
            {
                if (visual.SelfOrVisualAncestor<ListBoxItem>() is { } item)
                {
                    (DataContext as FancyEditorsTablesToolGroupViewModel)!.OpenTable((item.DataContext as TableItemViewModel)!);
                }
            }
        }
    }

    private void Search_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = (DataContext as FancyEditorsTablesToolGroupViewModel)!.OpenSelected();
        }
        else if (e.Key is Key.Down or Key.Up)
        {
            SelectionAdapter.HandleKeyDown(e);
            e.Handled = true;
        }
    }
}
