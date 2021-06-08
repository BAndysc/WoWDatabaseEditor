using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemDeserializer
    {
        bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem);
    }

    [NonUniqueProvider]
    public interface ISolutionItemDeserializer<T> : ISolutionItemDeserializer where T : ISolutionItem
    {
    }
}