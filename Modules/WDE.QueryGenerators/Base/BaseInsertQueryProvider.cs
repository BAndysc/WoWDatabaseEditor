using WDE.Common.Database;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

public abstract class BaseInsertQueryProvider<T> : IInsertQueryProvider<T>
{
    public IQuery Insert(T t)
    {
        return Queries.Table(TableName).Insert(Convert(t));
    }

    public IQuery BulkInsert(IReadOnlyCollection<T> collection)
    {
        return Queries.Table(TableName).BulkInsert(collection.Select(Convert));
    }

    protected abstract object Convert(T obj);

    public abstract DatabaseTable TableName { get; }
}