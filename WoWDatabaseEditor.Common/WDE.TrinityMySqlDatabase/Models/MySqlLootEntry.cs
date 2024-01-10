using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models;

public abstract class BaseMySqlLootEntry : ILootEntry
{
    public abstract LootSourceType SourceType { get; }

    [PrimaryKey]
    [Column(Name = "Entry")]
    public uint Entry { get; set; }

    public abstract int ItemOrCurrencyId { get; }

    [Column(Name = "Reference")]
    public uint Reference { get; set; }
    
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

public abstract class BaseMySqlLootEntryNoCurrency : BaseMySqlLootEntry
{
    [Column(Name = "Item")]
    public uint Item { get; set; }

    public override int ItemOrCurrencyId => (int)Item;
}

public abstract class BaseMySqlLootEntryWithCurrency : BaseMySqlLootEntry
{
    [Column(Name = "Item")]
    public uint Item { get; set; }
    
    [Column(Name = "IsCurrency")]
    public bool IsCurrency { get; set; }

    public override int ItemOrCurrencyId => (int)Item * (IsCurrency ? -1 : 1);
}

#region No-Currency Loot
[Table(Name = "creature_loot_template")]
public class CreatureLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Creature;
}

[Table(Name = "gameobject_loot_template")]
public class GameObjectLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.GameObject;
}

[Table(Name = "item_loot_template")]
public class ItemLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Item;
}

[Table(Name = "mail_loot_template")]
public class MailLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Mail;
}

[Table(Name = "fishing_loot_template")]
public class FishingLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Fishing;
}

[Table(Name = "skinning_loot_template")]
public class SkinningLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Skinning;
}

[Table(Name = "prospecting_loot_template")]
public class ProspectingLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Prospecting;
}

[Table(Name = "milling_loot_template")]
public class MillingLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Milling;
}

[Table(Name = "disenchant_loot_template")]
public class DisenchantLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Disenchant;
}

[Table(Name = "reference_loot_template")]
public class ReferenceLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Reference;
}

[Table(Name = "pickpocketing_loot_template")]
public class PickpocketingLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Pickpocketing;
}

[Table(Name = "spell_loot_template")]
public class SpellLootTemplate : BaseMySqlLootEntryNoCurrency
{
    public override LootSourceType SourceType => LootSourceType.Spell;
}
#endregion

#region Currency Loot
[Table(Name = "creature_loot_template")]
public class CreatureLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Creature;
}

[Table(Name = "gameobject_loot_template")]
public class GameObjectLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.GameObject;
}

[Table(Name = "item_loot_template")]
public class ItemLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Item;
}

[Table(Name = "mail_loot_template")]
public class MailLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Mail;
}

[Table(Name = "fishing_loot_template")]
public class FishingLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Fishing;
}

[Table(Name = "skinning_loot_template")]
public class SkinningLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Skinning;
}

[Table(Name = "prospecting_loot_template")]
public class ProspectingLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Prospecting;
}

[Table(Name = "milling_loot_template")]
public class MillingLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Milling;
}

[Table(Name = "disenchant_loot_template")]
public class DisenchantLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Disenchant;
}

[Table(Name = "reference_loot_template")]
public class ReferenceLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Reference;
}

[Table(Name = "pickpocketing_loot_template")]
public class PickpocketingLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Pickpocketing;
}

[Table(Name = "spell_loot_template")]
public class SpellLootTemplateWithCurrency : BaseMySqlLootEntryWithCurrency
{
    public override LootSourceType SourceType => LootSourceType.Spell;
}
#endregion