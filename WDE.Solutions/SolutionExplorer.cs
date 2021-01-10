using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.Common.Windows;
using WDE.Solutions.Explorer.ViewModels;
using WDE.Solutions.Explorer.Views;

namespace WDE.Solutions
{
    [AutoRegister, SingleInstance]
    public class SolutionExplorer : ISolutionExplorer, IToolProvider
    {
        private readonly SolutionExplorerViewModel _solutionExplorerViewModel;
        public bool AllowMultiple => false;

        public string Name => "Solution explorer";

        public SolutionExplorer(SolutionExplorerViewModel solutionExplorerViewModel)
        {
            _solutionExplorerViewModel = solutionExplorerViewModel;
        }
        
        public ITool Provide()
        {
            return _solutionExplorerViewModel;
        }

        public bool CanOpenOnStart => true;
    }
}
