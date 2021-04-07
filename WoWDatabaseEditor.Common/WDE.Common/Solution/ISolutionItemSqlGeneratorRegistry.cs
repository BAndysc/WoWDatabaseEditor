using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemSqlGeneratorRegistry
    {
        Task<string> GenerateSql(ISolutionItem item);
    }
}