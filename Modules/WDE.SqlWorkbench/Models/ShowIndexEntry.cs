using WDE.SqlWorkbench.Models.DataTypes;

namespace WDE.SqlWorkbench.Models;

internal readonly struct ShowIndexEntry
{
    public readonly bool NonUnique;
    public readonly string KeyName;
    public readonly long SeqInIndex;
    public readonly string ColumnName;
    public readonly bool? Ascending;
    public readonly long? SubPart;
    public readonly bool CanContainNull;
    public readonly string? Comment;
    public readonly string? IndexComment;
    public readonly bool Visible;
    public readonly IndexKind Kind;
    public readonly IndexType Type;

    public ShowIndexEntry(bool nonUnique, string keyName, long seqInIndex, string columnName, bool? ascending, long? subPart, bool canContainNull, IndexKind kind, IndexType type, string? comment, string? indexComment, bool visible)
    {
        NonUnique = nonUnique;
        KeyName = keyName;
        SeqInIndex = seqInIndex;
        ColumnName = columnName;
        Ascending = ascending;
        SubPart = subPart;
        CanContainNull = canContainNull;
        Kind = kind;
        Type = type;
        Comment = comment;
        IndexComment = indexComment;
        Visible = visible;
    }
}