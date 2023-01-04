using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Components;
using WDE.Solutions.Explorer.ViewModels;

namespace WDE.CommonViews.Avalonia.Solutions.Explorer.Views
{
    /// <summary>
    ///     Interaction logic for SolutionExplorerView
    /// </summary>
    public partial class SolutionExplorerView : ToolView 
    {
        public SolutionExplorerView()
        {
            InitializeComponent();
        }

        private void Tv_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is SolutionExplorerViewModel vm && vm.SelectedItem != null)
                vm.RequestOpenItem.Execute(vm.SelectedItem);
        }

        private void Tv_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            TreeView? tv = sender as TreeView;
            if (tv != null && e.Source is ScrollContentPresenter)
            {
                tv.UnselectAll();   
            }
        }
    }
}