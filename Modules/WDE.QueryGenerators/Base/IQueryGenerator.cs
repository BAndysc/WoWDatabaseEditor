using WDE.Common.Database;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

public interface IQueryGenerator<T>
{
    IQuery? TryInsert(T element);
    IQuery? TryBulkInsert(IReadOnlyCollection<T> elements);
    IQuery? TryDelete(T element);
    /// <summary>Deletes every row of the element's key/group (see <see cref="IDeleteAllQueryProvider{T}"/>).</summary>
    IQuery? TryDeleteAll(T element);
    IQuery? TryUpdate(T element);
    DatabaseTable? TableName { get; }
}