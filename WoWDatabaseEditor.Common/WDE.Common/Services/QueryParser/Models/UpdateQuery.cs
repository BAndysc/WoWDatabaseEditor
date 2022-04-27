using System.Collections.Generic;

namespace WDE.Common.Services.QueryParser.Models;

public class UpdateQuery : IBaseQuery
{
    public UpdateQuery(string tableName, IReadOnlyList<UpdateElement> updates, WhereCondition where)
    {
        TableName = tableName;
        Updates = updates;
        Where = where;
    }

    public string TableName { get; }
    public IReadOnlyList<UpdateElement> Updates { get; }
    public WhereCondition Where { get; }
}