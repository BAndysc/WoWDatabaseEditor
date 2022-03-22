namespace WDE.SqlInterpreter.Models;

public class EqualityWhereCondition
{
    public EqualityWhereCondition(string[] columns, object?[] values)
    {
        Columns = columns;
        Values = values;
    }
        
    public EqualityWhereCondition(string column, object? value)
    {
        Columns = new[]{column};
        Values = new[]{value};
    }

    public string[] Columns { get; }
    public object?[] Values { get; }
}