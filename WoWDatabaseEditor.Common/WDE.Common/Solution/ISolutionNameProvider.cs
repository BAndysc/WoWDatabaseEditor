using System.Threading.Tasks;
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

    [NonUniqueProvider]
    public interface ISolutionNameProviderAsync
    {
    }

    [NonUniqueProvider]
    public interface ISolutionNameProviderAsync<T> : ISolutionNameProviderAsync where T : ISolutionItem
    {
        Task<string> GetNameAsync(T item);
    }
}