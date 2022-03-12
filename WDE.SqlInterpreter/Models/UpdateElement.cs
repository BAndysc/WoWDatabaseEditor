namespace WDE.SqlInterpreter.Models;

public class UpdateElement
{
    public UpdateElement(string columnName)
    {
        ColumnName = columnName;
    }

    public string ColumnName { get; }
}