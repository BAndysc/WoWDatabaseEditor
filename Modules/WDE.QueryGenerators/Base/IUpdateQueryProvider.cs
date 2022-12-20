using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

[NonUniqueProvider]
public interface IUpdateQueryProvider<T>
{
    IQuery Update(T t);
    int Priority => 0;
}