using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using WDE.Common.Avalonia;
using WDE.DatabaseEditors.ViewModels;

namespace WDE.DatabaseEditors.Avalonia.Views;

public class TablesListToolView : UserControl
{
    public TablesListToolView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox itemsBox && e.Source is IVisual visual)
        {
            if (visual.SelfOrVisualAncestor<ListBoxItem>() is { } item)
            {
                (DataContext as TablesListToolViewModel)!.OpenTable((item.DataContext as TableItemViewModel)!);
            }
        }
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is ListBox itemsBox && e.Source is IVisual visual)
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
            if (sender is ListBox itemsBox && e.Source is IVisual visual)
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
            (DataContext as TablesListToolViewModel)!.OpenOnly();
        }
    }
}