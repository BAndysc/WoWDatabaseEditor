using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemSerializer
    {
    }
    
    [NonUniqueProvider]
    public interface ISolutionItemSerializer<T> : ISolutionItemSerializer where T : ISolutionItem
    {
        ISmartScriptProjectItem? Serialize(T item, bool forMostRecentlyUsed);
    }
}