using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

public abstract class BaseLootTemplate : ILootEntry
{
    public abstract LootSourceType SourceType { get; }
    
    [PrimaryKey]
    [Column(Name = "entry")]
    public uint Entry { get; set; }
    
    [Column(Name = "item")]
    public int ItemOrCurrencyId { get; set; }
    
    public uint Reference => MinCountOrReference < 0 ? (uint)-MinCountOrReference : 0;

    [Column(Name = "ChanceOrQuestChance")]
    public float Chance { get; set; }

    public bool QuestRequired => Chance < 0;

    public uint LootMode { get; set; }

    [Column(Name = "groupid")]
    public ushort GroupId { get; set; }

    public int MinCount => MinCountOrReference >= 0 ? MinCountOrReference : 1;
    
    [Column(Name = "maxcount")]
    public uint MaxCount { get; set; }

    public int BadLuckProtectionId => 0;

    public uint Build => 0;

    public virtual string? Comment { get; set; }
    
    public ushort MinPatch => 0;
    
    public ushort MaxPatch => 0;
    
    [Column(Name = "mincountOrRef")]
    public int MinCountOrReference { get; set; }
    
    [Column(Name = "condition_id")]
    public uint ConditionId { get; set; }
}

public abstract class BaseLootTemplateWithComment : BaseLootTemplate
{
    [Column(Name = "comments")]
    public override string? Comment { get; set; }
}

[Table(Name = "creature_loot_template")]
public class CreatureLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.Creature;
}

[Table(Name = "gameobject_loot_template")]
public class GameObjectLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.GameObject;
}

[Table(Name = "item_loot_template")]
public class ItemLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.Item;
}

[Table(Name = "mail_loot_template")]
public class MailLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.Mail;
}

[Table(Name = "fishing_loot_template")]
public class FishingLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.Fishing;
}

[Table(Name = "skinning_loot_template")]
public class SkinningLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.Skinning;
}

[Table(Name = "prospecting_loot_template")]
public class ProspectingLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.Prospecting;
}

[Table(Name = "milling_loot_template")]
public class MillingLootTemplate : BaseLootTemplate
{
    public override LootSourceType SourceType => LootSourceType.Milling;
}

[Table(Name = "disenchant_loot_template")]
public class DisenchantLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.Disenchant;
}

[Table(Name = "reference_loot_template")]
public class ReferenceLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.Reference;
}

[Table(Name = "pickpocketing_loot_template")]
public class PickpocketingLootTemplate : BaseLootTemplateWithComment
{
    public override LootSourceType SourceType => LootSourceType.Pickpocketing;
}

[Table(Name = "spell_loot_template")]
public class SpellLootTemplate : BaseLootTemplate
{
    public override LootSourceType SourceType => LootSourceType.Spell;
}