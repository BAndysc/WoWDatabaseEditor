using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Exceptions;
using WDE.LootEditor.Editor;
using WDE.LootEditor.Models;
using WDE.Module.Attributes;

namespace WDE.LootEditor.DataLoaders;

[AutoRegister]
public class LootLoader : ILootLoader
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly ILootEditorFeatures editorFeatures;

    public LootLoader(IDatabaseProvider databaseProvider,
        ILootEditorFeatures editorFeatures)
    {
        this.databaseProvider = databaseProvider;
        this.editorFeatures = editorFeatures;
    }

    public async Task<IReadOnlyList<LootEntry>> GetLootEntries(LootSourceType type, uint solutionItemEntry, uint difficulty)
    {
        switch (type)
        {
            case LootSourceType.Creature:
            {
                var template = await databaseProvider.GetCreatureTemplate(solutionItemEntry);
                if (template == null)
                    throw new UserException("Couldn't find creature template with entry " + solutionItemEntry);

                var loots = Enumerable.Range(0, template.LootCount)
                    .Select(idx => new LootEntry(template.GetLootId(idx)))
                    .Where(x => x != default)
                    .ToArray();
                if (loots.Length == 0 || difficulty > 0)
                {
                    // fallback to creature_template_difficulty
                    var difficulties = await databaseProvider.GetCreatureTemplateDifficulties(solutionItemEntry);
                    var defaultDifficulty = difficulties.FirstOrDefault(x => x.DifficultyId == difficulty);
                    if (defaultDifficulty != null || difficulty > 0)
                        return new LootEntry[] { new LootEntry(defaultDifficulty?.LootId ?? 0) };
                    
                    return new LootEntry[] { new LootEntry(solutionItemEntry) }; // default to creature entry
                }
                return loots;
            }
            case LootSourceType.Skinning:
            {
                var template = await databaseProvider.GetCreatureTemplate(solutionItemEntry);
                if (template == null)
                    throw new UserException("Couldn't find creature template with entry " + solutionItemEntry);

                if (template.SkinningLootId == 0 || difficulty > 0)
                {
                    var difficulties = await databaseProvider.GetCreatureTemplateDifficulties(solutionItemEntry);
                    var defaultDifficulty = difficulties.FirstOrDefault(x => x.DifficultyId == difficulty);
                    if (defaultDifficulty != null || difficulty > 0)
                        return new LootEntry[] { new(defaultDifficulty?.SkinningLootId ?? 0) };
                }
                
                return new LootEntry[] { new(template.SkinningLootId) };
            }
            case LootSourceType.Pickpocketing:
            {
                var template = await databaseProvider.GetCreatureTemplate(solutionItemEntry);
                if (template == null)
                    throw new UserException("Couldn't find creature template with entry " + solutionItemEntry);

                if (template.PickpocketLootId == 0 || difficulty > 0)
                {
                    var difficulties = await databaseProvider.GetCreatureTemplateDifficulties(solutionItemEntry);
                    var defaultDifficulty = difficulties.FirstOrDefault(x => x.DifficultyId == difficulty);
                    if (defaultDifficulty != null || difficulty > 0)
                        return new LootEntry[] { new(defaultDifficulty?.PickpocketLootId ?? 0) };
                }
                
                return new LootEntry[] { new(template.PickpocketLootId) };
            }
            case LootSourceType.GameObject:
            {
                var template = await databaseProvider.GetGameObjectTemplate(solutionItemEntry);
                if (template == null)
                    throw new UserException("Couldn't find gameobject template with entry " + solutionItemEntry);

                var lootId = template.GetLootId();
                if (!lootId.HasValue)
                    throw new UserException($"Gameobject {template.Name} ({template.Entry}) is {template.Type} and doesn't have loot");

                return new LootEntry[] { new(lootId.Value) };
            }
            case LootSourceType.Mail:
            case LootSourceType.Fishing:
            case LootSourceType.Item:
            case LootSourceType.Spell:
            case LootSourceType.Reference:
            case LootSourceType.Milling:
            case LootSourceType.Prospecting:
            case LootSourceType.Alter:
            case LootSourceType.Treasure:
                return new LootEntry[] { new(solutionItemEntry) };
            case LootSourceType.Disenchant:
                return new LootEntry[] { }; // note: this should be loaded from dbc itemsparse
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public async Task<IReadOnlyList<LootModel>> FetchLoot(LootSourceType type, LootEntry entry)
    {
        var loot = await databaseProvider.GetLoot(type, (uint)entry);
        IDatabaseProvider.ConditionKeyMask mask = IDatabaseProvider.ConditionKeyMask.SourceGroup;
        var key = new IDatabaseProvider.ConditionKey(editorFeatures.GetConditionSourceTypeFor(type), (int)(uint)entry, null, null);
        var rawConditions = await databaseProvider.GetConditionsForAsync(mask, key);
        var conditions = rawConditions.GroupBy(c => c.SourceEntry).ToDictionary(x => x.Key, x => x.ToList());
        
        List<LootModel> result = new();
        
        foreach (var lootEntry in loot)
        {
            if (conditions.TryGetValue(lootEntry.ItemOrCurrencyId, out var cond))
                result.Add(new LootModel(new AbstractLootEntry(lootEntry), cond?.Select(x => new AbstractCondition(x)).ToList()));
            else
                result.Add(new LootModel(new AbstractLootEntry(lootEntry)));
        }

        return result;
    }

    public Task<ILootTemplateName?> GetLootTemplateName(LootSourceType type, LootEntry entry)
    {
        return databaseProvider.GetLootTemplateName(type, (uint)entry);
    }
}