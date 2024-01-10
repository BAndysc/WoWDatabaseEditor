using System;

namespace WDE.Common.Database;

public interface ILootEntry
{
    LootSourceType SourceType { get; }
    uint Entry { get; }
    int ItemOrCurrencyId { get; }
    uint Reference { get; }
    float Chance { get; }
    bool QuestRequired { get; }
    uint LootMode { get; }
    ushort GroupId { get; }
    int MinCount { get; }
    uint MaxCount { get; }
    int BadLuckProtectionId { get; }
    uint Build { get; }
    uint ConditionId { get; }
    string? Comment { get; }
    ushort MinPatch { get; }
    ushort MaxPatch { get; }
}

public interface ITreasureLootEntry { }

public struct AbstractLootEntry : ILootEntry
{
    public LootSourceType SourceType { get; set; }
    public uint Entry { get; set; }
    public int ItemOrCurrencyId { get; set; }
    public uint Reference { get; set; }
    public float Chance { get; set; }
    public uint LootMode { get; set; }
    public bool QuestRequired { get; set; }
    public ushort GroupId { get; set; }
    public int MinCount { get; set; }
    public uint MaxCount { get; set; }
    public uint Build { get; set; }
    public uint ConditionId { get; set; }
    public int BadLuckProtectionId { get; set; }
    public string? Comment { get; set; }
    public ushort MinPatch { get; set; }
    public ushort MaxPatch { get; set; }

    public AbstractLootEntry(ILootEntry x)
    {
        SourceType = x.SourceType;
        Entry = x.Entry;
        ItemOrCurrencyId = x.ItemOrCurrencyId;
        Reference = x.Reference;
        Chance = x.Chance;
        LootMode = x.LootMode;
        QuestRequired = x.QuestRequired;
        GroupId = x.GroupId;
        MinCount = x.MinCount;
        MaxCount = x.MaxCount;
        BadLuckProtectionId = x.BadLuckProtectionId;
        Build = x.Build;
        ConditionId = x.ConditionId;
        Comment = x.Comment;
        MinPatch = x.MinPatch;
        MaxPatch = x.MaxPatch;
    }
}

public interface ILootTemplateName
{
    public uint Entry { get; }
    public string Name { get; }
    
    public bool DontLoadRecursively { get; }
}

public struct AbstractLootTemplateName : ILootTemplateName
{
    public uint Entry { get; set; }
    public string Name { get; set; }
    public bool DontLoadRecursively { get; set; }
}

public static class LootExtensions
{
    public static bool IsReference(this ILootEntry entry)
        => entry.Reference > 0;
    
    public static bool IsCurrency(this ILootEntry entry)
        => entry.ItemOrCurrencyId < 0;
    
    public static bool IsItem(this ILootEntry entry)
        => !entry.IsCurrency();
}

public enum LootSourceType
{
    Creature,
    GameObject,
    Item,
    Mail,
    Fishing,
    Skinning,
    Treasure,
    Alter,
    Spell,
    Reference,
    Disenchant,
    Milling,
    Prospecting,
    Pickpocketing,
}

public readonly struct LootEntry : IEquatable<LootEntry>
{
    private readonly uint entry;

    public LootEntry(uint entry)
    {
        this.entry = entry;
    }

    public bool Equals(LootEntry other)
    {
        return entry == other.entry;
    }

    public override bool Equals(object? obj)
    {
        return obj is LootEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)entry;
    }

    public static bool operator ==(LootEntry left, LootEntry right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LootEntry left, LootEntry right)
    {
        return !left.Equals(right);
    }

    public static explicit operator uint(LootEntry entry) => entry.entry;
    
    public override string ToString() => entry.ToString();
}