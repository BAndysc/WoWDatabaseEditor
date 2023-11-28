using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls.OptimizedVeryFastTableView;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Views;

public partial class SelectResultsView : UserControl
{
    public SelectResultsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void VirtualizedVeryFastTableView_OnValueUpdateRequest(string newValue)
    {
        if (DataContext is SelectResultsViewModel vm)
        {
            vm.UpdateSelectedCells(newValue);
        }
    }
}