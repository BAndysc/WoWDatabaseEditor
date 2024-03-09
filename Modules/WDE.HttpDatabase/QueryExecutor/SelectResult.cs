namespace WDE.HttpDatabase;

public struct SelectResult
{
    public List<string> Columns { get; init; }
    public List<MySqlType> Types { get; init; }
    public List<List<object?>> Rows { get; init; }
}