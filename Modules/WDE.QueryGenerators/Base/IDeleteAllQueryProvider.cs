using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

/// <summary>Deletes EVERY row belonging to the element's key/group (e.g. all spawn_group members of
/// the element's group id), as opposed to <see cref="IDeleteQueryProvider{T}"/> which deletes the
/// single row. Used for idempotent "rewrite the whole group" queries: DELETE all, then bulk INSERT.</summary>
[NonUniqueProvider]
public interface IDeleteAllQueryProvider<T>
{
    IQuery DeleteAll(T t);
    int Priority => 0;
}
