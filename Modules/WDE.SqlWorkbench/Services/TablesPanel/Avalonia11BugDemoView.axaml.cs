using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Prism.Commands;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;

namespace WDE.SqlWorkbench.Services.TablesPanel;

public partial class Avalonia11BugDemoView : UserControl
{
    private VirtualizedTreeView TablesTreeView = null!;
    public ICommand FocusTextBoxCommand { get; }

    public Avalonia11BugDemoView()
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
                e.Handled = true;
            }
        }
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is VirtualizedTreeView itemsBox)
            {
                if (itemsBox.SelectedNode is INodeType { } item)
                {
                }
            }
            e.Handled = true;
        }
    }

    private void Search_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
        }
        else if (e.Key is Key.Down or Key.Up)
        {
            TablesTreeView.Focus();
            TablesTreeView.RaiseEvent(e);
            e.Handled = true;
        }
    }
}