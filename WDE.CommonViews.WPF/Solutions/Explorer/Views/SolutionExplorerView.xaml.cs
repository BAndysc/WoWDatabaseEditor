using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WDE.Solutions.Explorer.ViewModels;

namespace WDE.Solutions.Explorer.Views
{
    /// <summary>
    ///     Interaction logic for SolutionExplorerView
    /// </summary>
    public partial class SolutionExplorerView : UserControl
    {
        public SolutionExplorerView()
        {
            InitializeComponent();
        }

        private void Tv_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not Grid g)
                return;

            if (VisualTreeHelper.GetParent(g) is not ScrollViewer)
                return;

            if (DataContext is SolutionExplorerViewModel vm && vm.SelectedItem != null)
            {
                // wpf shit...
                vm.SelectedItem.IsSelected = false;
                vm.SelectedItem = null;
            }
        }
    }
}