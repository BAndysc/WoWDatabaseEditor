using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqKit;
using LinqToDB;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.CommonModels;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.TrinityMySqlDatabase.Models;

namespace WDE.TrinityMySqlDatabase.Database;

public class TrinityCataMySqlDatabaseProvider : BaseTrinityMySqlDatabaseProvider<TrinityCataDatabase>
{
    public TrinityCataMySqlDatabaseProvider(IWorldDatabaseSettingsProvider settings,
        IAuthDatabaseSettingsProvider authSettings, DatabaseLogger databaseLogger,
        ICurrentCoreVersion currentCoreVersion) : base(settings, authSettings, databaseLogger, currentCoreVersion)
    {
    }

    public override void ConnectOrThrow()
    {
        using var model = Database();
        _ = model.CreatureTemplate.FirstOrDefault();
    }

    public override async Task<ICreatureTemplate?> GetCreatureTemplate(uint entry)
    {
        await using var model = Database();
        return await model.CreatureTemplate.FirstOrDefaultAsync(ct => ct.Entry == entry);
    }

    public override IReadOnlyList<ICreatureTemplate> GetCreatureTemplates()
    {
        using var model = Database();
        return model.CreatureTemplate.OrderBy(t => t.Entry).ToList<ICreatureTemplate>();
    }

    public override async Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync()
    {
        await using var model = Database();
        return await model.CreatureTemplate.OrderBy(t => t.Entry).ToListAsync<ICreatureTemplate>();
    }

    public override async Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry)
    {
        return Array.Empty<ICreatureTemplateDifficulty>();
    }

    public override async Task<IReadOnlyList<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId)
    {
        await using var model = Database();
        return await model.GossipMenuOptions.Where(option => option.MenuId == menuId).ToListAsync<IGossipMenuOption>();
    }

    public override List<IGossipMenuOption> GetGossipMenuOptions(uint menuId)
    {
        using var model = Database();
        return model.GossipMenuOptions.Where(option => option.MenuId == menuId).ToList<IGossipMenuOption>();
    }

    public override async Task<IReadOnlyList<IBroadcastText>> GetBroadcastTextsAsync()
    {
        await using var model = Database();
        return await (from t in model.BroadcastTexts orderby t.Id select t).ToListAsync<IBroadcastText>();
    }

    public override IBroadcastText? GetBroadcastTextByText(string text)
    {
        using var model = Database();
        return (from t in model.BroadcastTexts where t.Text == text || t.Text1 == text select t).FirstOrDefault();
    }

    public override async Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text)
    {
        await using var model = Database();
        return await (from t in model.BroadcastTexts where t.Text == text || t.Text1 == text select t)
            .FirstOrDefaultAsync();
    }

    public override async Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id)
    {
        await using var model = Database();
        return await (from t in model.BroadcastTexts where t.Id == id select t).FirstOrDefaultAsync();
    }

    public override async Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text)
    {
        await using var model = Database();
        return await model.BroadcastTextLocale.FirstOrDefaultAsync(b => b.Text == text || b.Text1 == text);
    }

    public override async Task<ICreature?> GetCreatureByGuidAsync(uint entry, uint guid)
    {
        await using var model = Database();
        return await model.Creature.FirstOrDefaultAsync(c => c.Guid == guid);
    }

    public override async Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid)
    {
        await using var model = Database();
        return await model.GameObject.FirstOrDefaultAsync(g => g.Guid == guid);
    }

    public override IEnumerable<ICreature> GetCreaturesByEntry(uint entry)
    {
        using var model = Database();
        return model.Creature.Where(g => g.Entry == entry).ToList();
    }

    public override IReadOnlyList<ICreature> GetCreatures()
    {
        using var model = Database();
        return model.Creature.OrderBy(t => t.Entry).ToList<ICreature>();
    }

    public override async Task<IReadOnlyList<ICreature>> GetCreaturesByEntryAsync(uint entry)
    {
        await using var model = Database();
        return await model.Creature.Where(g => g.Entry == entry).ToListAsync<ICreature>();
    }

    public override async Task<IReadOnlyList<IGameObject>> GetGameObjectsByEntryAsync(uint entry)
    {
        await using var model = Database();
        return await model.GameObject.Where(g => g.Entry == entry).ToListAsync<IGameObject>();
    }

    public override async Task<IReadOnlyList<ICreature>> GetCreaturesAsync()
    {
        await using var model = Database();
        return await model.Creature.OrderBy(t => t.Entry).ToListAsync<ICreature>();
    }

    public override async Task<IReadOnlyList<ICreature>> GetCreaturesAsync(IEnumerable<SpawnKey> guids)
    {
        await using var model = Database();
        var array = guids.Select(x => x.Guid).ToArray();
        return await model.Creature.Where(c => array.Contains(c.Guid)).ToListAsync<ICreature>();
    }
        
    public override async Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync(IEnumerable<SpawnKey> guids)
    {
        await using var model = Database();
        var array = guids.Select(x => x.Guid).ToArray();
        return await model.GameObject.Where(c => array.Contains(c.Guid)).ToListAsync<IGameObject>();
    }

    public override async Task<IReadOnlyList<ITrinityString>> GetStringsAsync()
    {
        await using var model = Database();
        return await model.Strings.ToListAsync<ITrinityString>();
    }

    public override async Task<IReadOnlyList<IDatabaseSpellDbc>> GetSpellDbcAsync()
    {
        await using var model = Database();
        return await model.SpellDbc.ToListAsync<IDatabaseSpellDbc>();
    }

    protected override async Task SetCreatureTemplateAI(TrinityCataDatabase model, uint entry, string ainame,
        string scriptname)
    {
        await model.CreatureTemplate.Where(p => p.Entry == entry)
            .Set(p => p.AIName, ainame)
            .Set(p => p.ScriptName, scriptname)
            .UpdateAsync();
    }

    protected override async Task<ICreature?> GetCreatureByGuidAsync(TrinityCataDatabase model, uint guid)
    {
        return await model.Creature.FirstOrDefaultAsync(e => e.Guid == guid);
    }

    public override async Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync()
    {
        await using var model = Database();
        return await model.GameObject.ToListAsync<IGameObject>();
    }

    public override IEnumerable<IGameObject> GetGameObjects()
    {
        using var model = Database();
        return model.GameObject.ToList<IGameObject>();
    }

    public override IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry)
    {
        using var model = Database();
        return model.GameObject.Where(g => g.Entry == entry).ToList();
    }

    protected override Task<IGameObject?> GetGameObjectByGuidAsync(TrinityCataDatabase model, uint guid)
    {
        return model.GameObject.FirstOrDefaultAsync<IGameObject>(g => g.Guid == guid);
    }

    public override async Task<IReadOnlyList<ICreature>> GetCreaturesByMapAsync(int map)
    {
        await using var model = Database();
        return await model.Creature.Where(c => c.Map == map).ToListAsync<ICreature>();
    }

    public override async Task<IReadOnlyList<IGameObject>> GetGameObjectsByMapAsync(int map)
    {
        await using var model = Database();
        return await model.GameObject.Where(c => c.Map == map).ToListAsync<IGameObject>();
    }

    private IQueryable<MySqlCataQuestTemplate> GetQuestsQuery(TrinityCataDatabase model)
    {
        return (from t in model.CataQuestTemplate
            join addon in model.CataQuestTemplateAddon on t.Entry equals addon.Entry into adn
            from subaddon in adn.DefaultIfEmpty()
            orderby t.Entry
            select t.SetAddon(subaddon));
    }

    private IQueryable<MySqlCataQuestTemplate> GetQuestsQueryByZoneSortId(TrinityCataDatabase model, int zoneSortId)
    {
        return (from t in model.CataQuestTemplate
            where t.QuestSortId == zoneSortId
            join addon in model.CataQuestTemplateAddon on t.Entry equals addon.Entry into adn
            from subaddon in adn.DefaultIfEmpty()
            orderby t.Entry
            select t.SetAddon(subaddon));
    }

    public override IReadOnlyList<IQuestTemplate> GetQuestTemplates()
    {
        using var model = Database();

        return GetQuestsQuery(model).ToList<IQuestTemplate>();
    }

    public override async Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesAsync()
    {
        await using var model = Database();
        return await GetQuestsQuery(model).ToListAsync<IQuestTemplate>();
    }

    public override async Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesBySortIdAsync(int questSortId)
    {
        await using var model = Database();
        return await GetQuestsQueryByZoneSortId(model, questSortId).ToListAsync<IQuestTemplate>();
    }

    public override async Task<IQuestTemplate?> GetQuestTemplate(uint entry)
    {
        await using var model = Database();
        MySqlCataQuestTemplateAddon? addon =
            await model.CataQuestTemplateAddon.FirstOrDefaultAsync(addon => addon.Entry == entry);
        return (await model.CataQuestTemplate.FirstOrDefaultAsync(q => q.Entry == entry))?.SetAddon(addon);
    }

    public override async Task<IReadOnlyList<ICreatureModelInfo>> GetCreatureModelInfoAsync()
    {
        await using var model = Database();
        return await model.CreatureModelInfo.ToListAsync<ICreatureModelInfo>();
    }

    public override async Task<ICreatureModelInfo?> GetCreatureModelInfo(uint displayId)
    {
        using var model = Database();
        return model.CreatureModelInfo.FirstOrDefault(x => x.DisplayId == displayId);
    }

    public override async Task<ICreature?> GetCreaturesByGuidAsync(uint entry, uint guid)
    {
        await using var model = Database();
        return await model.Creature.FirstOrDefaultAsync(x => x.Guid == guid);
    }

    public override async Task<IReadOnlyList<ICreatureAddon>> GetCreatureAddons()
    {
        await using var model = Database();
        return await model.CreatureAddon.ToListAsync<ICreatureAddon>();
    }

    public override async Task<IReadOnlyList<ICreatureTemplateAddon>> GetCreatureTemplateAddons()
    {
        await using var model = Database();
        return await model.CreatureTemplateAddon.ToListAsync<ICreatureTemplateAddon>();
    }

    public override async Task<ICreatureAddon?> GetCreatureAddon(uint entry, uint guid)
    {
        await using var model = Database();
        return await model.CreatureAddon.FirstOrDefaultAsync<ICreatureAddon>(x => x.Guid == guid);
    }

    public override async Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry)
    {
        await using var model = Database();
        return await model.CreatureTemplateAddon.FirstOrDefaultAsync<ICreatureTemplateAddon>(x => x.Entry == entry);
    }

    public override async Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId)
    {
        await using var model = Database();
        return await model.WaypointDataCata.Where(wp => wp.PathId == pathId).OrderBy(wp => wp.PointId)
            .ToListAsync<IWaypointData>();
    }

    public override async Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId, uint count)
    {
        await using var model = Database();
        return await model.SmartScriptWaypointCata.Where(wp => wp.PathId >= pathId && wp.PathId < pathId + count).OrderBy(wp => wp.PointId)
            .ToListAsync<ISmartScriptWaypoint>();
    }

    public override async Task<IReadOnlyList<IEventScriptLine>> FindEventScriptLinesBy(
        IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions)
    {
        await using var model = Database();
        var events = await model.EventScripts
            .Where(GenerateWhereConditionsForEventScript<MySqlEventScriptNoCommentLine>(conditions))
            .ToListAsync<IEventScriptLine>();
        var spells = await model.SpellScripts
            .Where(GenerateWhereConditionsForEventScript<MySqlSpellScriptNoCommentLine>(conditions))
            .ToListAsync<IEventScriptLine>();
        var waypoints = await model.WaypointScripts
            .Where(GenerateWhereConditionsForEventScript<MySqlWaypointScriptNoCommentLine>(conditions))
            .ToListAsync<IEventScriptLine>();
        return events.Concat(spells).Concat(waypoints).ToList();
    }

    public override async Task<IReadOnlyList<IEventScriptLine>> GetEventScript(EventScriptType type, uint id)
    {
        await using var model = Database();
        switch (type)
        {
            case EventScriptType.Event:
                return await model.EventScripts.Where(s => s.Id == id).ToListAsync<IEventScriptLine>();
            case EventScriptType.Spell:
                return await model.SpellScripts.Where(s => s.Id == id).ToListAsync<IEventScriptLine>();
            case EventScriptType.Waypoint:
                return await model.WaypointScripts.Where(s => s.Id == id).ToListAsync<IEventScriptLine>();
            case EventScriptType.Gossip:
            case EventScriptType.QuestStart:
            case EventScriptType.QuestEnd:
            case EventScriptType.GameObjectUse:
                return new List<IEventScriptLine>();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public override IEnumerable<ISmartScriptLine> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type)
    {
        using var model = Database();
        return model.SmartScript.Where(line => line.EntryOrGuid == entryOrGuid && line.ScriptSourceType == (int)type)
            .ToList();
    }

    public override async Task<IReadOnlyList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid,
        SmartScriptType type)
    {
        await using var model = Database();
        return await model.SmartScript
            .Where(line => line.EntryOrGuid == entryOrGuid && line.ScriptSourceType == (int)type)
            .ToListAsync<ISmartScriptLine>();
    }

    public override async Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(
        IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch
            )> conditions)
    {
        await using var model = Database();
        var predicate = PredicateBuilder.New<MySqlSmartScriptLine>();
        foreach (var value in conditions)
        {
            if (value.what == IDatabaseProvider.SmartLinePropertyType.Action)
            {
                if (value.parameterIndex == 1)
                    predicate = predicate.Or(o =>
                        o.ActionType == value.whatValue && o.ActionParam1 == value.valueToSearch);
                else if (value.parameterIndex == 2)
                    predicate = predicate.Or(o =>
                        o.ActionType == value.whatValue && o.ActionParam2 == value.valueToSearch);
                else if (value.parameterIndex == 3)
                    predicate = predicate.Or(o =>
                        o.ActionType == value.whatValue && o.ActionParam3 == value.valueToSearch);
                else if (value.parameterIndex == 4)
                    predicate = predicate.Or(o =>
                        o.ActionType == value.whatValue && o.ActionParam4 == value.valueToSearch);
                else if (value.parameterIndex == 5)
                    predicate = predicate.Or(o =>
                        o.ActionType == value.whatValue && o.ActionParam5 == value.valueToSearch);
                else if (value.parameterIndex == 6)
                    predicate = predicate.Or(o =>
                        o.ActionType == value.whatValue && o.ActionParam6 == value.valueToSearch);
            }
            else if (value.what == IDatabaseProvider.SmartLinePropertyType.Event)
            {
                if (value.parameterIndex == 1)
                    predicate = predicate.Or(
                        o => o.EventType == value.whatValue && o.EventParam1 == value.valueToSearch);
                else if (value.parameterIndex == 2)
                    predicate = predicate.Or(
                        o => o.EventType == value.whatValue && o.EventParam2 == value.valueToSearch);
                else if (value.parameterIndex == 3)
                    predicate = predicate.Or(
                        o => o.EventType == value.whatValue && o.EventParam3 == value.valueToSearch);
                else if (value.parameterIndex == 4)
                    predicate = predicate.Or(
                        o => o.EventType == value.whatValue && o.EventParam4 == value.valueToSearch);
            }
            else if (value.what == IDatabaseProvider.SmartLinePropertyType.Target)
            {
                if (value.parameterIndex == 1)
                    predicate = predicate.Or(o =>
                        o.TargetType == value.whatValue && o.TargetParam1 == value.valueToSearch);
                else if (value.parameterIndex == 2)
                    predicate = predicate.Or(o =>
                        o.TargetType == value.whatValue && o.TargetParam2 == value.valueToSearch);
                else if (value.parameterIndex == 3)
                    predicate = predicate.Or(o =>
                        o.TargetType == value.whatValue && o.TargetParam3 == value.valueToSearch);
            }
        }

        return await model.SmartScript.Where(predicate).ToListAsync<ISmartScriptLine>();
    }

    public override async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)>
        GetCreatureLootCrossReference(uint lootId)
    {
        await using var database = Database();
        var creatures = await database.CreatureTemplate.Where(x => x.LootId == lootId).ToListAsync();
        return (creatures, Array.Empty<ICreatureTemplateDifficulty>());
    }

    public override async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)>
        GetCreatureSkinningLootCrossReference(uint lootId)
    {
        await using var database = Database();
        var creatures = await database.CreatureTemplate.Where(x => x.SkinningLootId == lootId).ToListAsync();
        return (creatures, Array.Empty<ICreatureTemplateDifficulty>());
    }

    public override async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)>
        GetCreaturePickPocketLootCrossReference(uint lootId)
    {
        await using var database = Database();
        var creatures = await database.CreatureTemplate.Where(x => x.PickpocketLootId == lootId).ToListAsync();
        return (creatures, Array.Empty<ICreatureTemplateDifficulty>());
    }

    public override async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type)
    {
        await using var database = Database();
        switch (type)
        {
            case LootSourceType.Item:
                return await database.ItemLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.GameObject:
                return await database.GameObjectLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Fishing:
                return await database.FishingLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Pickpocketing:
                return await database.PickpocketingLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Skinning:
                return await database.SkinningLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Disenchant:
                return await database.DisenchantLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Prospecting:
                return await database.ProspectingLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Milling:
                return await database.MillingLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Reference:
                return await database.ReferenceLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Creature:
                return await database.CreatureLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Mail:
                return await database.MailLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            case LootSourceType.Spell:
                return await database.SpellLootTemplateWithCurrency.OrderBy(x => x.Entry)
                    .ThenBy(x => x.GroupId)
                    .ThenBy(x => x.Item)
                    .ToListAsync<ILootEntry>();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public override async Task<IReadOnlyList<ILootEntry>> GetReferenceLootCrossReference(uint lootId)
    {
        if (lootId == 0)
            return Array.Empty<ILootEntry>();
        await using var database = Database();
        var loot = new[]
        {
            await database.CreatureLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.GameObjectLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.ItemLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.FishingLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.PickpocketingLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.SkinningLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.DisenchantLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.ProspectingLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.MillingLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.MailLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.SpellLootTemplateWithCurrency.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
            await database.ReferenceLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
        };
        return loot.SelectMany(x => x).ToList();
    }

    public override async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type, uint entry)
    {
        await using var database = Database();
        switch (type)
        {
            case LootSourceType.Item:
                return await database.ItemLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.GameObject:
                return await database.GameObjectLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Fishing:
                return await database.FishingLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Pickpocketing:
                return await database.PickpocketingLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Skinning:
                return await database.SkinningLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Disenchant:
                return await database.DisenchantLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Prospecting:
                return await database.ProspectingLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Milling:
                return await database.MillingLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Reference:
                return await database.ReferenceLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Creature:
                return await database.CreatureLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Mail:
                return await database.MailLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            case LootSourceType.Spell:
                return await database.SpellLootTemplateWithCurrency.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

}