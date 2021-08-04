using WDE.Common;
using WDE.Module.Attributes;

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