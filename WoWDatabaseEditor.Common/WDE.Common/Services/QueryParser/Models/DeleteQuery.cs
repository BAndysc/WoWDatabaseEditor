using WDE.Common.Database;

namespace WDE.Common.Services.QueryParser.Models;

public class DeleteQuery : IBaseQuery
{
    public DeleteQuery(DatabaseTable tableName, WhereCondition where)
    {
        TableName = tableName;
        Where = where;
    }

    public DatabaseTable TableName { get; }
    public WhereCondition Where { get; }
}