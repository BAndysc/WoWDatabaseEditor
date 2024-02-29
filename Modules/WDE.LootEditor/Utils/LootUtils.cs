using System;
using WDE.Common.Database;
using WDE.LootEditor.Solution;
using WDE.LootEditor.Solution.PerEntity;

namespace WDE.LootEditor.Utils;

public static class LootUtils
{
    public static bool SolutionEntryIsLootEntry(this LootSourceType type)
    {
        switch (type)
        {
            case LootSourceType.Fishing:
            case LootSourceType.Item:
            case LootSourceType.Spell:
            case LootSourceType.Reference:
            case LootSourceType.Milling:
            case LootSourceType.Prospecting:
            case LootSourceType.Alter:
            case LootSourceType.Mail:
            case LootSourceType.Disenchant: // it is not, but disenchantId is in dbc and can't be edited
                return true;
            case LootSourceType.Creature:
            case LootSourceType.GameObject:
            case LootSourceType.Skinning:
            case LootSourceType.Treasure:
            case LootSourceType.Pickpocketing:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    public static bool CanUpdateSourceLootEntry(this LootSourceType type)
    {
        switch (type)
        {
            // because loot entry == solution entry
            case LootSourceType.Fishing:
            case LootSourceType.Item:
            case LootSourceType.Spell:
            case LootSourceType.Reference:
            case LootSourceType.Milling:
            case LootSourceType.Prospecting:
            case LootSourceType.Alter:
            case LootSourceType.Mail:
                return false;
            // because loot entry = disenchantId which is in dbc
            case LootSourceType.Disenchant:
                return false;
            // because loot entry is stored in gameobject_template.data1, which should always be sniffed
            case LootSourceType.GameObject:
                return false;
            case LootSourceType.Creature:
            case LootSourceType.Skinning:
            case LootSourceType.Treasure:
            case LootSourceType.Pickpocketing:
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    public static string GetParameterFor(this LootSourceType type)
    {
        switch (type)
        {
            case LootSourceType.Creature: return "CreatureParameter";
            case LootSourceType.GameObject: return "GameobjectParameter";
            case LootSourceType.Item: return "ItemParameter";
            case LootSourceType.Mail: return "MailTemplateParameter";
            case LootSourceType.Fishing: return "ZoneAreaParameter";
            case LootSourceType.Skinning: return "CreatureParameter";
            case LootSourceType.Treasure: return "LootTreasureParameter";
            case LootSourceType.Alter: return "ItemParameter";
            case LootSourceType.Spell: return "SpellParameter";
            case LootSourceType.Reference: return "LootReferenceParameter";
            case LootSourceType.Disenchant: return "ItemParameter";
            case LootSourceType.Milling: return "ItemParameter";
            case LootSourceType.Prospecting: return "ItemParameter";
            case LootSourceType.Pickpocketing: return "CreatureParameter";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    public static bool SolutionEntryIsLootEntry(this PerEntityLootSolutionItem solutionItem)
    {
        return solutionItem.Type.SolutionEntryIsLootEntry();
    }
}