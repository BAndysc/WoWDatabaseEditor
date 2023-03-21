using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Database.Counters;

[UniqueProvider]
public interface IDatabaseRowsCountProvider
{
    Task<int> GetRowsCountByPrimaryKey(string table, long primaryKey, CancellationToken token);
    Task<int> GetCreaturesCountByEntry(long entry, CancellationToken token);
}