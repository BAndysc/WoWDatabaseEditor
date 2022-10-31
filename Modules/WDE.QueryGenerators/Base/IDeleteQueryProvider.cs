using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

[NonUniqueProvider]
public interface IDeleteQueryProvider<T>
{
    IQuery Delete(T t);
    int Priority => 0;
}