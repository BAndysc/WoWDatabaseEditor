using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemNameRegistry
    {
        string GetName(ISolutionItem item);
        Task<string> GetNameAsync(ISolutionItem item);
    }
}