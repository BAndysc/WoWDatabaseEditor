
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WDE.Solutions.Explorer.ViewModels;

namespace WDE.Solutions.Explorer.Views
{
    /// <summary>
    ///     Interaction logic for SolutionExplorerView
    /// </summary>
    public class SolutionExplorerView : UserControl
    {
        public SolutionExplorerView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Tv_OnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SolutionExplorerViewModel vm && vm.SelectedItem != null)
                vm.RequestOpenItem.Execute(vm.SelectedItem);
        }
    }
}