using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.Common.Avalonia;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.Avalonia.Views;

public partial class TablesListToolView : UserControl
{
    private ISelectionAdapter SelectionAdapter = null!;
    
    public TablesListToolView()
    {
        InitializeComponent();
    }

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

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is ListBox itemsBox && e.Source is Visual visual)
        {
            if (visual.SelfOrVisualAncestor<ListBoxItem>() is { } item)
            {
                (DataContext as TablesListToolViewModel)!.OpenTable((item.DataContext as TableItemViewModel)!);
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
                    (DataContext as TablesListToolViewModel)!.OpenTable((item.DataContext as TableItemViewModel)!);
                }
            }
        }
    }

    private void Search_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = (DataContext as TablesListToolViewModel)!.OpenSelected();
        }
        else if (e.Key is Key.Down or Key.Up)
        {
            SelectionAdapter.HandleKeyDown(e);
            e.Handled = true;
        }
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox itemsBox && e.Source is Visual visual)
        {
            if (visual.SelfOrVisualAncestor<ListBoxItem>() is { } item)
            {
                (DataContext as TablesListToolViewModel)!.OpenTable((item.DataContext as TableItemViewModel)!);
            }
        }
    }

}