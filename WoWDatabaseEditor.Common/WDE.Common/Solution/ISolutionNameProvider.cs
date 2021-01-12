using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionNameProvider
    {
    }

    [NonUniqueProvider]
    public interface ISolutionNameProvider<T> : ISolutionNameProvider where T : ISolutionItem
    {
        string GetName(T item);
    }
}