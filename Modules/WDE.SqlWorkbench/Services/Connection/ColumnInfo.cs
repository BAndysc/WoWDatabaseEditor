namespace WDE.SqlWorkbench.Services.Connection;

internal readonly struct ColumnInfo
{
    public ColumnInfo(string name, string type, bool isNullable, bool isPrimaryKey, bool isAutoIncrement, object? defaultValue)
    {
        Name = name;
        Type = type;
        IsNullable = isNullable;
        IsPrimaryKey = isPrimaryKey;
        IsAutoIncrement = isAutoIncrement;
        DefaultValue = defaultValue;
    }

    public readonly string Name;
    public readonly string Type;
    public readonly bool IsNullable;
    public readonly bool IsPrimaryKey;
    public readonly bool IsAutoIncrement;
    public readonly object? DefaultValue;
}