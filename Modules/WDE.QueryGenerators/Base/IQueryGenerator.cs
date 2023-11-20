using WDE.Common.Database;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

public interface IQueryGenerator<T>
{
    IQuery? TryInsert(T element);
    IQuery? TryBulkInsert(IReadOnlyCollection<T> elements);
    IQuery? TryDelete(T element);
    IQuery? TryUpdate(T element);
    DatabaseTable? TableName { get; }
}