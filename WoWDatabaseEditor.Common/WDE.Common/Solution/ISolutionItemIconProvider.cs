using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemIconProvider
    {
    }
    
    public interface ISolutionItemIconProvider<in T> : ISolutionItemIconProvider where T : ISolutionItem
    {
        ImageUri GetIcon(T icon);
    }
}