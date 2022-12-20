using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

[NonUniqueProvider]
public interface IInsertQueryProvider<T>
{
    IQuery Insert(T t);
    string TableName { get; }
    int Priority => 0;
}