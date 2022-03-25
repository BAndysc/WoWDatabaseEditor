namespace WDE.Common.Services.QueryParser.Models;

public class UpdateElement
{
    public UpdateElement(string columnName, object? value)
    {
        ColumnName = columnName;
        Value = value;
    }

    public string ColumnName { get; }
    public object? Value { get; }
}