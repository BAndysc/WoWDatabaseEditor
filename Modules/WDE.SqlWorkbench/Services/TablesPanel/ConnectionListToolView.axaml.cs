using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Prism.Commands;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;

namespace WDE.SqlWorkbench.Services.TablesPanel;

public partial class ConnectionListToolView : UserControl
{
    private VirtualizedTreeView TablesTreeView = null!;
    public ICommand FocusTextBoxCommand { get; }
    
    public ConnectionListToolView()
    {
        FocusTextBoxCommand = new DelegateCommand(() => this.GetControl<TextBox>("SearchTextBox").Focus());
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TablesTreeView = this.GetControl<VirtualizedTreeView>("VirtualizedTreeView");
        DispatcherTimer.RunOnce(() =>
        {
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            searchTextBox?.Focus();
            searchTextBox?.SelectAll();
        }, TimeSpan.FromMilliseconds(1));
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is VirtualizedTreeView itemsBox && e.Source is Visual visual)
        {
            if (visual.SelfOrVisualAncestor<VirtualizedTreeViewItem>() is { } item)
            {
                (DataContext as ConnectionListToolViewModel)!.OpenItem((item.DataContext as INodeType)!);
                e.Handled = true;
            }
        }
    }
    // disable single tap to open for now
    // private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    // {
    //     if (e.InitialPressMouseButton != MouseButton.Left)
    //         return;
    //     
    //     if (sender is VirtualizedTreeView itemsBox && e.Source is IVisual visual)
    //     {
    //         if (visual.SelfOrVisualAncestor<VirtualizedTreeViewItem>() is { } item)
    //         {
    //             (DataContext as ConnectionListToolViewModel)!.OpenItem((item.DataContext as INodeType)!);
    //         }
    //     }
    // }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is VirtualizedTreeView itemsBox)
            {
                if (itemsBox.SelectedNode is INodeType { } item)
                {
                    (DataContext as ConnectionListToolViewModel)!.OpenItem(item);
                }
            }
            e.Handled = true;
        }
    }

    private void Search_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = (DataContext as ConnectionListToolViewModel)!.OpenSelected();
        }
        else if (e.Key is Key.Down or Key.Up)
        {
            TablesTreeView.Focus();
            TablesTreeView.RaiseEvent(e);
            e.Handled = true;
        }
    }
}