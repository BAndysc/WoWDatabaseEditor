
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Windows;
using Prism.Ioc;

namespace WoWDatabaseEditor.Managers
{
    [WDE.Common.Attributes.AutoRegister, WDE.Common.Attributes.SingleInstance]
    public class SolutionExplorerView : IToolProvider
    {
        private readonly Lazy<ISolutionExplorer> _solutionExplorer;

        public bool AllowMultiple => false;

        public string Name => "Solution explorer";

        public SolutionExplorerView(Lazy<ISolutionExplorer> solutionExplorer)
        {
            _solutionExplorer = solutionExplorer;
        }
        
        public ContentControl GetView()
        {
            return _solutionExplorer.Value.GetSolutionExplorerView();
        }
    }
}
