using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.Solutions.Explorer.ViewModels;

namespace WDE.Solutions
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionExplorer : ISolutionExplorer, IToolProvider
    {
        private readonly SolutionExplorerViewModel solutionExplorerViewModel;

        public SolutionExplorer(SolutionExplorerViewModel solutionExplorerViewModel)
        {
            this.solutionExplorerViewModel = solutionExplorerViewModel;
        }

        public bool AllowMultiple => false;

        public string Name => "Solution explorer";

        public ITool Provide()
        {
            return solutionExplorerViewModel;
        }

        public bool CanOpenOnStart => true;
    }
}