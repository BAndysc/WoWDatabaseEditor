using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface ISolutionExplorer
    {
        ContentControl GetSolutionExplorerView();
    }
}
