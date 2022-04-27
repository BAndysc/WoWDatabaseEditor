namespace WDE.Common.Services.QueryParser.Models;

public class EqualityWhereCondition
{
    public EqualityWhereCondition(string[] columns, object?[] values, string rawSql)
    {
        Columns = columns;
        Values = values;
        RawSql = rawSql;
    }
        
    public EqualityWhereCondition(string column, object? value, string rawSql)
    {
        Columns = new[]{column};
        Values = new[]{value};
        RawSql = rawSql;
    }

    public string[] Columns { get; }
    public object?[] Values { get; }
    public string RawSql { get; }
}