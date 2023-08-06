using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

[NonUniqueProvider]
public interface IUpdateQueryProvider<T>
{
    IQuery Update(T diff);
    string TableName { get; }
    int Priority => 0;
}