using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

[NonUniqueProvider]
public interface IUpdateQueryProvider<T>
{
    IQuery Update(T diff);
    int Priority => 0;
}