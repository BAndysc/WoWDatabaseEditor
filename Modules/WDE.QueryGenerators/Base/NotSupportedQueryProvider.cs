using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

[AutoRegister]
[SingleInstance]
public abstract class NotSupportedQueryProvider<T> : IInsertQueryProvider<T>, IDeleteQueryProvider<T>, IUpdateQueryProvider<T>
{
    public abstract string TableName { get; }
    
    public IQuery Insert(T t) => throw new TableNotSupportedException(TableName);

    public IQuery BulkInsert(IReadOnlyCollection<T> collection) => throw new TableNotSupportedException(TableName);

    public IQuery Delete(T t) => throw new TableNotSupportedException(TableName);

    public IQuery Update(T diff) => throw new TableNotSupportedException(TableName);
}

public class TableNotSupportedException : Exception
{
    public string TableName { get; }
    
    public TableNotSupportedException(string tableName) : base("Currently selected database doesn't support table " + tableName)
    {
        TableName = tableName;
    }
}