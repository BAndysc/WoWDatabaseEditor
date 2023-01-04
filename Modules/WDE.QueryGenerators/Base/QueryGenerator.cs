using WDE.Common;
using WDE.Common.Database;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

internal class QueryGenerator<R> : IQueryGenerator<R>
{
    private IInsertQueryProvider<R>? insertProvider;
    private IDeleteQueryProvider<R>? deleteProvider;
    private IUpdateQueryProvider<R>? updateProvider;
    
    public QueryGenerator(IEnumerable<IInsertQueryProvider<R>> insertProviders,
        IEnumerable<IDeleteQueryProvider<R>> deleteProviders,
        IEnumerable<IUpdateQueryProvider<R>> updateProviders)
    {
        insertProvider = insertProviders.MaxBy(p => p.Priority);
        
        deleteProvider = deleteProviders.MaxBy(p => p.Priority);

        updateProvider = updateProviders.MaxBy(p => p.Priority);

        if (insertProvider == null && deleteProvider == null && updateProvider == null)
            LOG.LogError("Couldn't find a provider for " + typeof(R));
    }

    public IQuery? TryInsert(R element) => insertProvider?.Insert(element);
    public IQuery? TryBulkInsert(IReadOnlyCollection<R> elements) => insertProvider?.BulkInsert(elements);

    public IQuery? TryDelete(R element) => deleteProvider?.Delete(element);
    public IQuery? TryUpdate(R element) => updateProvider?.Update(element);

    public DatabaseTable? TableName => insertProvider?.TableName;
}