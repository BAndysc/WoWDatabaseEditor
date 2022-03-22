namespace WDE.SqlInterpreter.Models;

public class DeleteQuery : IBaseQuery
{
    public DeleteQuery(string tableName, WhereCondition where)
    {
        TableName = tableName;
        Where = where;
    }

    public string TableName { get; }
    public WhereCondition Where { get; }
}