using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

public interface IQueryGenerator<R>
{
    IQuery? Insert(R element);
    IQuery? Delete(R element);
    IQuery? Update(R element);
    string? TableName { get; }
}