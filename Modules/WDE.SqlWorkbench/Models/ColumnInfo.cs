namespace WDE.SqlWorkbench.Models;

internal readonly struct ColumnInfo
{
    public ColumnInfo(string name, string type, bool isNullable, bool isPrimaryKey, bool isAutoIncrement, string? defaultValue, string? charset, string? collation)
    {
        Name = name;
        Type = type;
        IsNullable = isNullable;
        IsPrimaryKey = isPrimaryKey;
        IsAutoIncrement = isAutoIncrement;
        DefaultValue = defaultValue;
        Charset = charset;
        Collation = collation;
    }

    public readonly string Name;
    public readonly string Type;
    public readonly bool IsNullable;
    public readonly bool IsPrimaryKey;
    public readonly bool IsAutoIncrement;
    public readonly string? DefaultValue;
    public readonly string? Charset;
    public readonly string? Collation;
}