using System.Collections.Generic;
using WDE.Common.Database;

namespace WDE.Common.Services.QueryParser.Models;

public class UpdateQuery : IBaseQuery
{
    public UpdateQuery(DatabaseTable tableName, IReadOnlyList<UpdateElement> updates, WhereCondition where)
    {
        TableName = tableName;
        Updates = updates;
        Where = where;
    }

    public DatabaseTable TableName { get; }
    public IReadOnlyList<UpdateElement> Updates { get; }
    public WhereCondition Where { get; }
}