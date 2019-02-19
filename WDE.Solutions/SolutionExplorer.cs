using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.Common.Windows;
using WDE.Solutions.Explorer.Views;

namespace WDE.Solutions
{
    [AutoRegister, SingleInstance]
    public class SolutionExplorer : ISolutionExplorer, IToolProvider
    {
        public bool AllowMultiple => false;

        public string Name => "Solution explorer";

        public bool CanOpenOnStart => true;

        public ContentControl GetSolutionExplorerView()
        {
            return new SolutionExplorerView();
        }

        public ContentControl GetView()
        {
            return GetSolutionExplorerView();
        }
    }
}
