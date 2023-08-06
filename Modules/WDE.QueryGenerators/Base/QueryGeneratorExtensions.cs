using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

public static class QueryGeneratorExtensions
{
    public static IQuery Insert<T>(this IQueryGenerator<T> generator, T element)
    {
        return generator.TryInsert(element) ?? throw new QueryGeneratorException<T>(nameof(Insert));
    }
    
    public static IQuery BulkInsert<T>(this IQueryGenerator<T> generator, IReadOnlyCollection<T> elements)
    {
        return generator.TryBulkInsert(elements) ?? throw new QueryGeneratorException<T>(nameof(BulkInsert));
    }
    
    public static IQuery Delete<T>(this IQueryGenerator<T> generator, T element)
    {
        return generator.TryDelete(element) ?? throw new QueryGeneratorException<T>(nameof(Delete));
    }
    
    public static IQuery Update<T>(this IQueryGenerator<T> generator, T element)
    {
        return generator.TryUpdate(element) ?? throw new QueryGeneratorException<T>(nameof(Update));
    }
}