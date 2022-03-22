using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemSqlGeneratorRegistry
    {
        Task<IQuery> GenerateSql(ISolutionItem item);
        Task<IList<(ISolutionItem, IQuery)>> GenerateSplitSql(ISolutionItem item);
    }
}