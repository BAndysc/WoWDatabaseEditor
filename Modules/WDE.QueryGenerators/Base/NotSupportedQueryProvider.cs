using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

[AutoRegister]
[SingleInstance]
public abstract class NotSupportedQueryProvider<T> : IInsertQueryProvider<T>, IDeleteQueryProvider<T>, IUpdateQueryProvider<T>
{
    public abstract DatabaseTable TableName { get; }
    
    public IQuery Insert(T t) => throw new TableNotSupportedException(TableName);

    public IQuery BulkInsert(IReadOnlyCollection<T> collection) => throw new TableNotSupportedException(TableName);

    public IQuery Delete(T t) => throw new TableNotSupportedException(TableName);

    public IQuery Update(T diff) => throw new TableNotSupportedException(TableName);
}

public class TableNotSupportedException : Exception
{
    public DatabaseTable TableName { get; }
    
    public TableNotSupportedException(DatabaseTable tableName) : base("Currently selected database doesn't support table " + tableName)
    {
        TableName = tableName;
    }
}