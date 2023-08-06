using WDE.SqlQueryGenerator;
using Extensions = AvaloniaStyles.Controls.Extensions;

namespace WDE.QueryGenerators.Base;

public interface IQueryGenerator<T>
{
    IQuery? TryInsert(T element);
    IQuery? TryBulkInsert(IReadOnlyCollection<T> elements);
    IQuery? TryDelete(T element);
    IQuery? TryUpdate(T element);
    string? TableName { get; }
}