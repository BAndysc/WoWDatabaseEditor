using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemIconRegistry
    {
        ImageUri GetIcon(ISolutionItem item);
    }
}