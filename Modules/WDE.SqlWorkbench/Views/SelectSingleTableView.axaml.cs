using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaStyles.Controls.OptimizedVeryFastTableView;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Views;

public partial class SelectSingleTableView : UserControl
{
    public SelectSingleTableView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private IDisposable? timer;
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // why: when the active tab (in Dock) is changed, this control is rebuild
        // and during this process, the Viewport is not yet set (it is size 0), but the Offset tries to be set
        // unfortunately this causes the scroll to be set to 0,0, because it is clamped to the size
        // this is a workaround, upon attaching to the view, we delay the scroll by 1ms, so the Viewport is set
        // and updating the ScrollOffset works.
        timer = DispatcherTimer.RunOnce(() =>
        {
            if (DataContext is SelectSingleTableViewModel vm)
            {
                vm.ScrollOffset += new Vector(0, 0.00001f);
            }
            timer = null;
        }, TimeSpan.FromMilliseconds(1));
        SetColumnsWidthToContent();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        timer?.Dispose();
        timer = null;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        SetColumnsWidthToContent();
    }
    
    private void SetColumnsWidthToContent()
    {
        DispatcherTimer.RunOnce(() =>
        {
            if (DataContext is SelectResultsViewModel vm &&
                !vm.ColumnsHeaderAlreadySetAutoSizeWidth)
            {
                vm.ColumnsHeaderAlreadySetAutoSizeWidth = true;
                this.GetControl<VirtualizedVeryFastTableView>("Table").AutoFitColumnsWidth();
            }
        }, TimeSpan.FromMilliseconds(1));
    }

    private void VirtualizedVeryFastTableView_OnColumnPressed(object? sender, ColumnPressedEventArgs e)
    {
        if (e.ColumnIndex == 0) // that's the special fake # column
            return;
        
        if (DataContext is SelectSingleTableViewModel vm)
        {
            vm.SortBy(e.ColumnIndex);
        }
    }

    private void VirtualizedVeryFastTableView_OnValueUpdateRequest(string newValue)
    {
        if (DataContext is SelectResultsViewModel vm)
        {
            vm.UpdateSelectedCells(newValue);
        }
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SelectSingleTableViewModel vm)
        {
            if (vm.Selection.Empty)
            {
                vm.AddRowCommand.Execute();
            }
        }
    }
}