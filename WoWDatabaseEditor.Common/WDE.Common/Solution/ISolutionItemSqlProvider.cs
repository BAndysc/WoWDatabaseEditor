using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemSqlProvider
    {
    }

    [NonUniqueProvider]
    public interface ISolutionItemSqlProvider<T> : ISolutionItemSqlProvider where T : ISolutionItem
    {
        Task<IQuery> GenerateSql(T item);
    }
}