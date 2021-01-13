using WDE.Common;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.Solutions.Explorer.ViewModels;

namespace WDE.Solutions
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionExplorer : ISolutionExplorer
    {
        public SolutionExplorer()
        {
        }
    }
}