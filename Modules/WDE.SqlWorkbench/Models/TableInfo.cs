namespace WDE.SqlWorkbench.Models;

internal readonly struct TableInfo
{
    public readonly string Schema;
    public readonly string Name;
    public readonly TableType Type;
    public readonly string? Engine;
    public readonly string? RowFormat;
    public readonly string? Collation;
    public readonly ulong? DataLength;
    public readonly string? Comment;

    public TableInfo(string schema, string name, TableType type, string? engine, string? rowFormat, string? collation, ulong? dataLength, string? comment)
    {
        Schema = schema;
        Name = name;
        Type = type;
        Engine = engine;
        RowFormat = rowFormat;
        Collation = collation;
        DataLength = dataLength;
        Comment = comment;
    }
}