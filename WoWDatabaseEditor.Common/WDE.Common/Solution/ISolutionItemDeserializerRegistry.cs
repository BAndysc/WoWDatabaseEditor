using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemDeserializerRegistry
    {
        bool TryDeserialize(ISmartScriptProjectItem item, out ISolutionItem? solutionItem);
    }
}