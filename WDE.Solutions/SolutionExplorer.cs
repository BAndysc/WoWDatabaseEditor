using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Common;
using WDE.Solutions.Explorer.Views;

namespace WDE.Solutions
{
    [WDE.Common.Attributes.AutoRegister]
    public class SolutionExplorer : ISolutionExplorer
    {
        public ContentControl GetSolutionExplorerView()
        {
            return new SolutionExplorerView();
        }
    }
}
