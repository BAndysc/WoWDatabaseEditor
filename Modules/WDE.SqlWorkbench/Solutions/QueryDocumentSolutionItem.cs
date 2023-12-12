using System;
using System.Collections.ObjectModel;
using WDE.Common;

namespace WDE.SqlWorkbench.Solutions;

public class QueryDocumentSolutionItem : ISolutionItem, IEquatable<QueryDocumentSolutionItem>
{
    public string FileName { get; }
    public Guid ConnectionId { get; }
    public bool IsTemporary { get; }
    
    public QueryDocumentSolutionItem(string fileName, Guid connectionId, bool isTemporary)
    {
        FileName = fileName;
        ConnectionId = connectionId;
        IsTemporary = isTemporary;
    }
    
    public bool IsContainer => false;
    public ObservableCollection<ISolutionItem>? Items => null;
    public string? ExtraId => null;
    public bool IsExportable => false;
    public ISolutionItem Clone() => new QueryDocumentSolutionItem(FileName, ConnectionId, IsTemporary);
    
    public bool Equals(QueryDocumentSolutionItem? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return FileName == other.FileName && ConnectionId.Equals(other.ConnectionId) && IsTemporary == other.IsTemporary;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((QueryDocumentSolutionItem)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileName, ConnectionId, IsTemporary);
    }

    public static bool operator ==(QueryDocumentSolutionItem? left, QueryDocumentSolutionItem? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(QueryDocumentSolutionItem? left, QueryDocumentSolutionItem? right)
    {
        return !Equals(left, right);
    }

    public QueryDocumentSolutionItem WithConnectionId(Guid connectionDataId)
    {
        return new QueryDocumentSolutionItem(FileName, connectionDataId, IsTemporary);
    }
}