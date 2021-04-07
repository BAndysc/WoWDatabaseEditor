using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemSqlProvider
    {
    }

    [NonUniqueProvider]
    public interface ISolutionItemSqlProvider<T> : ISolutionItemSqlProvider where T : ISolutionItem
    {
        Task<string> GenerateSql(T item);
    }
}