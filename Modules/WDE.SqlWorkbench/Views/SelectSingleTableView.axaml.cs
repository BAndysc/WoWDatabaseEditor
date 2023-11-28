using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
}