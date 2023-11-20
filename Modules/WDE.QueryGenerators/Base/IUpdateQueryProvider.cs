using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

[NonUniqueProvider]
public interface IUpdateQueryProvider<T>
{
    IQuery Update(T diff);
    DatabaseTable TableName { get; }
    int Priority => 0;
}