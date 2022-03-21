namespace WDE.SqlInterpreter.Models;

public class UpdateElement
{
    public UpdateElement(string columnName, string value)
    {
        ColumnName = columnName;
        Value = value;
    }

    public string ColumnName { get; }
    public string Value { get; }
}