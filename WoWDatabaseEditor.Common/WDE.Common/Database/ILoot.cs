namespace WDE.Common.Database;

public interface ILootEntry
{
    uint Entry { get; }
    int ItemOrCurrencyId { get; }
    float Chance { get; }
    uint LootMode { get; }
    ushort GroupId { get; }
    int MinCountOrRef { get; }
    uint MaxCount { get; }
    uint Build { get; }
    string? Comment { get; }
}

public struct AbstractLootEntry : ILootEntry
{
    public uint Entry { get; set; }
    public int ItemOrCurrencyId { get; set; }
    public float Chance { get; set; }
    public uint LootMode { get; set; }
    public ushort GroupId { get; set; }
    public int MinCountOrRef { get; set; }
    public uint MaxCount { get; set; }
    public uint Build { get; set; }
    public string? Comment { get; set; }
}

public static class LootExtensions
{
    public static bool IsReference(this ILootEntry entry)
        => entry.MinCountOrRef < 0;
    
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