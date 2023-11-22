using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.Common;
using WDE.Common.Database;

namespace WDE.LootEditor.Solution.PerDatabaseTable;

public class PerDatabaseTableLootSolutionItem : ISolutionItem, IEquatable<PerDatabaseTableLootSolutionItem>
{
    private readonly LootSourceType type;
 
    public PerDatabaseTableLootSolutionItem(LootSourceType type)
    {
        this.type = type;
    }

    public LootSourceType Type => type;
    
    public bool IsContainer => false;
    
    public ObservableCollection<ISolutionItem>? Items => null;
    
    public string? ExtraId => null;
    
    public bool IsExportable => true;
    
    public ISolutionItem Clone()
    {
        return new PerDatabaseTableLootSolutionItem(type);
    }
    
    public bool Equals(PerDatabaseTableLootSolutionItem? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return type == other.type;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PerDatabaseTableLootSolutionItem)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)type);
    }

    public static bool operator ==(PerDatabaseTableLootSolutionItem? left, PerDatabaseTableLootSolutionItem? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PerDatabaseTableLootSolutionItem? left, PerDatabaseTableLootSolutionItem? right)
    {
        return !Equals(left, right);
    }

}