using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemSerializerRegistry
    {
        ISmartScriptProjectItem? Serialize(ISolutionItem item, bool forMostRecentlyUsed);
    }
}