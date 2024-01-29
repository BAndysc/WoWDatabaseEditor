using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Editor;

[RequiresCore("CMaNGOS-TBC", "CMaNGOS-Classic", "CMaNGOS-WoTLK")]
[AutoRegister]
[SingleInstance]
public class CmangosLootEditorFeatures : ILootEditorFeatures
{
    private readonly ICurrentCoreVersion currentCoreVersion;

    public CmangosLootEditorFeatures(ICurrentCoreVersion currentCoreVersion)
    {
        this.currentCoreVersion = currentCoreVersion;
        if (this.currentCoreVersion.Current.Version.Build == 12340)
        {
            SupportedTypes = new[]
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
                LootSourceType.Prospecting,
                LootSourceType.Pickpocketing
            };
        }
        else if (this.currentCoreVersion.Current.Version.Build == 5875)
        {
            SupportedTypes = new[]
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
                LootSourceType.Pickpocketing
            };
        }
        else
        {
            SupportedTypes = new[]
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
                LootSourceType.Prospecting,
                LootSourceType.Pickpocketing
            };
        }
    }
    
    public bool HasPatchField => false;

    public bool HasLootModeField => false;
    
    public bool HasConditionId => true;
    
    public bool HasBadLuckProtectionId => false;

    public bool ItemCanBeCurrency => false;
    
    public bool LootGroupHasName(LootSourceType lootType) => lootType == LootSourceType.Reference;
    
    public bool LootGroupHasFlags(LootSourceType lootType) => false;

    public bool HasCommentField(LootSourceType lootType) => lootType is not LootSourceType.Spell and not LootSourceType.Milling;

    public int GetConditionSourceTypeFor(LootSourceType type) => 0;

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

    public IReadOnlyList<LootSourceType> SupportedTypes { get; }
}