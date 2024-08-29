using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models;

public abstract class BaseMasterMySqlLootEntry : ILootEntry
{
    public abstract LootSourceType SourceType { get; }

    [PrimaryKey]
    [Column(Name = "ItemType")]
    public LootType LootType { get; set; }

    [PrimaryKey]
    [Column(Name = "Entry")]
    public uint Entry { get; set; }

    [Column(Name = "Item")]
    public int ItemOrCurrencyId { get; set; }

    public uint Reference => LootType == LootType.Reference ? (uint)ItemOrCurrencyId : 0;
    
    [Column(Name = "Chance")]
    public float Chance { get; set; }
    
    [Column(Name = "QuestRequired")]
    public bool QuestRequired { get; set; }

    [Column(Name = "LootMode")]
    public uint LootMode { get; set; }
    
    [Column(Name = "GroupId")]
    public ushort GroupId { get; set; }
    
    [Column(Name = "MinCount")]
    public int MinCount { get; set; }
    
    [Column(Name = "MaxCount")]
    public uint MaxCount { get; set; }

    public int BadLuckProtectionId => 0;

    public uint Build => 0;

    [Column(Name = "Comment")]
    public string? Comment { get; set; }

    public ushort MinPatch => 0;

    public ushort MaxPatch => 0;

    public uint ConditionId => 0;
}

[Table(Name = "creature_loot_template")]
public class CreatureMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Creature;
}

[Table(Name = "gameobject_loot_template")]
public class GameObjectMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.GameObject;
}

[Table(Name = "item_loot_template")]
public class ItemMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Item;
}

[Table(Name = "mail_loot_template")]
public class MailMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Mail;
}

[Table(Name = "fishing_loot_template")]
public class FishingMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Fishing;
}

[Table(Name = "skinning_loot_template")]
public class SkinningMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Skinning;
}

[Table(Name = "prospecting_loot_template")]
public class ProspectingMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Prospecting;
}

[Table(Name = "milling_loot_template")]
public class MillingMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Milling;
}

[Table(Name = "disenchant_loot_template")]
public class DisenchantMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Disenchant;
}

[Table(Name = "reference_loot_template")]
public class ReferenceMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Reference;
}

[Table(Name = "pickpocketing_loot_template")]
public class PickpocketingMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Pickpocketing;
}

[Table(Name = "spell_loot_template")]
public class SpellMasterLootTemplate : BaseMasterMySqlLootEntry
{
    public override LootSourceType SourceType => LootSourceType.Spell;
}