using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Editor;

[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath", "Azeroth")]
[AutoRegister]
[SingleInstance]
public abstract class BaseTrinityLootEditorFeatures : ILootEditorFeatures
{
    public bool HasPatchField => false;
    
    public bool HasLootModeField => true;
    
    public bool HasConditionId => false;
    
    public bool HasBadLuckProtectionId => false;

    public abstract bool ItemCanBeCurrency { get; }

    public bool HasCommentField(LootSourceType lootType) => true;
    
    public bool LootGroupHasName(LootSourceType lootType) => false;
    
    public bool LootGroupHasFlags(LootSourceType lootType) => false;
    
    public int GetConditionSourceTypeFor(LootSourceType type)
    {
        switch (type)
        {
            case LootSourceType.Creature: return 1;
            case LootSourceType.GameObject: return 4;
            case LootSourceType.Item: return 5;
            case LootSourceType.Mail: return 6;
            case LootSourceType.Fishing: return 3;
            case LootSourceType.Skinning: return 11;
            case LootSourceType.Spell: return 12;
            case LootSourceType.Reference: return 10;
            case LootSourceType.Disenchant: return 2;
            case LootSourceType.Milling: return 7;
            case LootSourceType.Pickpocketing: return 8;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public int GetMaxLootEntryForType(LootSourceType type, uint difficultyId) => 1;

    public DatabaseTable GetTableNameFor(LootSourceType type)
    {
        return DatabaseTable.WorldTable(GetTableName(type));
    }
    
    private string GetTableName(LootSourceType type)
    {
        switch (type)
        {
            case LootSourceType.Creature: return "creature_loot_template";
            case LootSourceType.GameObject: return "gameobject_loot_template";
            case LootSourceType.Item: return "item_loot_template";
            case LootSourceType.Mail: return "mail_loot_template";
            case LootSourceType.Fishing: return "fishing_loot_template";
            case LootSourceType.Skinning: return "skinning_loot_template";
            case LootSourceType.Spell: return "spell_loot_template";
            case LootSourceType.Reference: return "reference_loot_template";
            case LootSourceType.Disenchant: return "disenchant_loot_template";
            case LootSourceType.Milling: return "milling_loot_template";
            case LootSourceType.Pickpocketing: return "pickpocketing_loot_template";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public IReadOnlyList<LootSourceType> SupportedTypes { get; } = new[]
    {
        LootSourceType.Creature,
        LootSourceType.GameObject,
        LootSourceType.Item,
        LootSourceType.Mail,
        LootSourceType.Fishing,
        LootSourceType.Skinning,
        LootSourceType.Spell,
        LootSourceType.Reference,
        LootSourceType.Disenchant,
        LootSourceType.Milling,
        LootSourceType.Pickpocketing
    };
}

[RequiresCore("TrinityMaster", "TrinityWrath", "Azeroth")]
[AutoRegister]
[SingleInstance]
public class TrinityLootEditorFeatures : BaseTrinityLootEditorFeatures
{
    public override bool ItemCanBeCurrency => false;
}

[RequiresCore("TrinityCata")]
[AutoRegister]
[SingleInstance]
public class TrinityCataLootEditorFeatures : BaseTrinityLootEditorFeatures
{
    public override bool ItemCanBeCurrency => true;
}
