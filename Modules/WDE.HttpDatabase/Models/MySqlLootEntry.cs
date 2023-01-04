
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonLootEntry : ILootEntry
{
    public LootSourceType SourceType { get; set; }
    public uint Entry { get; set; }
    public int ItemOrCurrencyId { get; set; }
    public uint Reference { get; set; }
    public float Chance { get; set; }
    public bool QuestRequired { get; set; }
    public uint LootMode { get; set; }
    public ushort GroupId { get; set; }
    public int MinCount { get; set; }
    public uint MaxCount { get; set; }
    public int BadLuckProtectionId { get; set; }
    public uint Build { get; set; }
    public uint ConditionId { get; set; }
    public string? Comment { get; set; }
    public ushort MinPatch { get; set; }
    public ushort MaxPatch { get; set; }
}
