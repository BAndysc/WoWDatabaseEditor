using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemSqlGeneratorRegistry
    {
        Task<string> GenerateSql(ISolutionItem item);
        Task<IList<(ISolutionItem, string)>> GenerateSplitSql(ISolutionItem item);
    }
}