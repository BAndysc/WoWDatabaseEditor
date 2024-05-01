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

public class TrinityMasterMySqlDatabaseProvider : BaseTrinityMySqlDatabaseProvider<TrinityMasterDatabase>
{
    public TrinityMasterMySqlDatabaseProvider(IWorldDatabaseSettingsProvider settings, IAuthDatabaseSettingsProvider authSettings, DatabaseLogger databaseLogger, ICurrentCoreVersion currentCoreVersion) : base(settings, authSettings, databaseLogger, currentCoreVersion)
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
        var template = await model.CreatureTemplate.FirstOrDefaultAsync(ct => ct.Entry == entry);
        if (template == null)
            return null;
        var models = await model.CreatureTemplateModel.Where(x => x.CreatureId == entry).ToListAsync();
        return template.WithModels(models);
    }

    public override IReadOnlyList<ICreatureTemplate> GetCreatureTemplates()
    {
        using var model = Database();
        var templates = model.CreatureTemplate.OrderBy(t => t.Entry).ToList();
        var models = model.CreatureTemplateModel.OrderBy(t => t.CreatureId).ThenBy(x => x.Index).ToList();
        MergeCreatureTemplateModels(templates, models);
        return templates;
    }

    /// <summary>
    /// Given a sorted lists of templates and models (sorted by Creature Entry), it merges models into templates
    /// </summary>
    /// <param name="templates"></param>
    /// <param name="models"></param>
    private void MergeCreatureTemplateModels(List<MySqlCreatureTemplateMaster> templates, List<CreatureTemplateModel> models)
    {
        int j = 0;
        for (int i = 0; i < templates.Count; ++i)
        {
            if (j >= models.Count)
                break;
            
            while (j < models.Count && models[j].CreatureId < templates[i].Entry)
                j++;

            var count = 0;
            while (j + count < models.Count && models[j + count].CreatureId == templates[i].Entry)
                count++;

            templates[i].WithModels(models.Skip(j).Take(count).ToList());
            j += count;
        }
    }

    public override async Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync()
    {
        await using var model = Database();
        var templates = await model.CreatureTemplate.OrderBy(t => t.Entry).ToListAsync();
        var models = await model.CreatureTemplateModel.OrderBy(t => t.CreatureId).ThenBy(x => x.Index).ToListAsync();
        MergeCreatureTemplateModels(templates, models);
        return templates;
    }
    
    public override async Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry)
    {
        await using var model = Database();
        return await model.CreatureTemplateDifficulty.Where(x => x.Entry == entry).ToListAsync<ICreatureTemplateDifficulty>();
    }

    public override async Task<IReadOnlyList<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId)
    {
        await using var model = Database();
        return await model.GossipMenuOptionsMaster.Where(option => option.MenuId == menuId).ToListAsync<IGossipMenuOption>();
    }
    
    public override List<IGossipMenuOption> GetGossipMenuOptions(uint menuId)
    {
        using var model = Database();
        return model.GossipMenuOptionsMaster.Where(option => option.MenuId == menuId).ToList<IGossipMenuOption>();
    }
    
    public override async Task<IReadOnlyList<IBroadcastText>> GetBroadcastTextsAsync()
    {
        return await Task.FromResult(new List<IBroadcastText>());
    }
    
    public override IBroadcastText? GetBroadcastTextByText(string text)
    {
        return null;
    }

    public override async Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text)
    {
        return null;
    }
    
    public override async Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id)
    {
        return null;
    }

    public override async Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text)
    {
        return null;
    }

    public override async Task<ICreature?> GetCreatureByGuidAsync(uint entry, uint guid)
    {
        await using var model = Database();
        return await model.Creature.FirstOrDefaultAsync(c => c.Guid == guid);
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
    
    protected override async Task SetCreatureTemplateAI(TrinityMasterDatabase model, uint entry, string ainame, string scriptname)
    {
        await model.CreatureTemplate.Where(p => p.Entry == entry)
            .Set(p => p.AIName, ainame)
            .Set(p => p.ScriptName, scriptname)
            .UpdateAsync();
    }

    protected override async Task<ICreature?> GetCreatureByGuidAsync(TrinityMasterDatabase model, uint guid)
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
    
    protected override Task<IGameObject?> GetGameObjectByGuidAsync(TrinityMasterDatabase model, uint guid)
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
    
    private IQueryable<MySqlMasterQuestTemplate> GetQuestsQuery(TrinityMasterDatabase model)
    {
        return (from t in model.MasterQuestTemplate
            join addon in model.QuestTemplateAddon on t.Entry equals addon.Entry into adn
            from subaddon in adn.DefaultIfEmpty()
            orderby t.Entry
            select t.SetAddon(subaddon));
    }

    private IQueryable<MySqlMasterQuestTemplate> GetQuestsQueryByZoneSortId(TrinityMasterDatabase model, int zoneSortId)
    {
        return (from t in model.MasterQuestTemplate
            where t.QuestSortId == zoneSortId
            join addon in model.QuestTemplateAddon on t.Entry equals addon.Entry into adn
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
        var addon = await model.QuestTemplateAddon.FirstOrDefaultAsync(addon => addon.Entry == entry);
        return (await model.MasterQuestTemplate.FirstOrDefaultAsync(q => q.Entry == entry))?.SetAddon(addon);
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

    public override async Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid)
    {
        await using var model = Database();
        return await model.GameObject.FirstOrDefaultAsync(x => x.Guid == guid);
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

    public override async Task<IReadOnlyList<IPlayerChoice>?> GetPlayerChoicesAsync()
    {
        await using var model = Database();
        return await model.PlayerChoice.ToListAsync<IPlayerChoice>();
    }

    public override async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync()
    {
        await using var model = Database();
        return await model.PlayerChoiceResponse.ToListAsync<IPlayerChoiceResponse>();
    }

    public override async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId)
    {
        await using var model = Database();
        return await model.PlayerChoiceResponse.Where(x => x.ChoiceId == choiceId).ToListAsync<IPlayerChoiceResponse>();
    }

    public override async Task<IReadOnlyList<IQuestObjective>> GetQuestObjectives(uint questId)
    {
        await using var model = Database();
        return await model.QuestObjective.Where(x => x.QuestId == questId).ToListAsync<IQuestObjective>();
    }

    public override async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex)
    {
        await using var model = Database();
        return await model.QuestObjective.FirstOrDefaultAsync(x => x.QuestId == questId && x.StorageIndex == storageIndex);
    }
    
    public override async Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId)
    {
        await using var model = Database();
        return await model.QuestObjective.FirstOrDefaultAsync(x => x.ObjectiveId == objectiveId);
    }

    public override async Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry)
    {
        await using var model = Database();
        return await model.AreaTriggerTemplate.FirstOrDefaultAsync(x => x.Id == entry);
    }

    public override async Task<IPhaseName?> GetPhaseNameAsync(uint phaseId)
    {
        await using var model = Database();
        return await model.PhaseNames.FirstOrDefaultAsync(x => x.Id == phaseId);
    }

    public override async Task<IReadOnlyList<IPhaseName>?> GetPhaseNamesAsync()
    {
        await using var model = Database();
        return await model.PhaseNames.ToListAsync<IPhaseName>();
    }

    public override IList<IPhaseName>? GetPhaseNames()
    {
        using var model = Database();
        return model.PhaseNames.ToList<IPhaseName>();
    }
    
    public override async Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId, uint count) => null;

    public override async Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId) => null;

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
            case EventScriptType.Gossip:
            case EventScriptType.QuestStart:
            case EventScriptType.QuestEnd:
            case EventScriptType.GameObjectUse:
                return new List<IEventScriptLine>();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    public override async Task<IReadOnlyList<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions)
    {
        await using var model = Database();
        var events = await model.EventScripts.Where(GenerateWhereConditionsForEventScript<MySqlEventScriptLine>(conditions)).ToListAsync<IEventScriptLine>();
        var spells = await model.SpellScripts.Where(GenerateWhereConditionsForEventScript<MySqlSpellScriptLine>(conditions)).ToListAsync<IEventScriptLine>();
        return events.Concat(spells).ToList();
    }
    
    public override IEnumerable<ISmartScriptLine> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type)
    {
        using var model = Database();
        return model.SmartScript.Where(line => line.EntryOrGuid == entryOrGuid && line.ScriptSourceType == (int) type).ToList();
    }
    
    public override async Task<IReadOnlyList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type)
    {
        await using var model = Database();
        return await model.SmartScript.Where(line => line.EntryOrGuid == entryOrGuid && line.ScriptSourceType == (int) type).ToListAsync<ISmartScriptLine>();
    }

    public override async Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions)
    {
        await using var model = Database();
        var predicate = PredicateBuilder.New<MasterMySqlSmartScriptLine>();
        foreach (var value in conditions)
        {
            if (value.what == IDatabaseProvider.SmartLinePropertyType.Action)
            {
                if (value.parameterIndex == 1)
                    predicate = predicate.Or(o => o.ActionType == value.whatValue && o.ActionParam1 == value.valueToSearch);
                else if (value.parameterIndex == 2)
                    predicate = predicate.Or(o => o.ActionType == value.whatValue && o.ActionParam2 == value.valueToSearch);
                else if (value.parameterIndex == 3)
                    predicate = predicate.Or(o => o.ActionType == value.whatValue && o.ActionParam3 == value.valueToSearch);
                else if (value.parameterIndex == 4)
                    predicate = predicate.Or(o => o.ActionType == value.whatValue && o.ActionParam4 == value.valueToSearch);
                else if (value.parameterIndex == 5)
                    predicate = predicate.Or(o => o.ActionType == value.whatValue && o.ActionParam5 == value.valueToSearch);
                else if (value.parameterIndex == 6)
                    predicate = predicate.Or(o => o.ActionType == value.whatValue && o.ActionParam6 == value.valueToSearch);
            }
            else if (value.what == IDatabaseProvider.SmartLinePropertyType.Event)
            {
                if (value.parameterIndex == 1)
                    predicate = predicate.Or(o => o.EventType == value.whatValue && o.EventParam1 == value.valueToSearch);
                else if (value.parameterIndex == 2)
                    predicate = predicate.Or(o => o.EventType == value.whatValue && o.EventParam2 == value.valueToSearch);
                else if (value.parameterIndex == 3)
                    predicate = predicate.Or(o => o.EventType == value.whatValue && o.EventParam3 == value.valueToSearch);
                else if (value.parameterIndex == 4)
                    predicate = predicate.Or(o => o.EventType == value.whatValue && o.EventParam4 == value.valueToSearch);
            }
            else if (value.what == IDatabaseProvider.SmartLinePropertyType.Target)
            {
                if (value.parameterIndex == 1)
                    predicate = predicate.Or(o => o.TargetType == value.whatValue && o.TargetParam1 == value.valueToSearch);
                else if (value.parameterIndex == 2)
                    predicate = predicate.Or(o => o.TargetType == value.whatValue && o.TargetParam2 == value.valueToSearch);
                else if (value.parameterIndex == 3)
                    predicate = predicate.Or(o => o.TargetType == value.whatValue && o.TargetParam3 == value.valueToSearch);
            }
        }
        return await model.SmartScript.Where(predicate).ToListAsync<ISmartScriptLine>();    
    }
    
    public override async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureLootCrossReference(uint lootId)
    {
        await using var database = Database();
        var difficulties = await database.CreatureTemplateDifficulty.Where(x => x.LootId == lootId).ToListAsync();
        return (Array.Empty<ICreatureTemplate>(), difficulties);
    }

    public override async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureSkinningLootCrossReference(uint lootId)
    {
        await using var database = Database();
        var difficulties = await database.CreatureTemplateDifficulty.Where(x => x.SkinningLootId == lootId).ToListAsync();
        return (Array.Empty<ICreatureTemplate>(), difficulties);
    }

    public override async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreaturePickPocketLootCrossReference(uint lootId)
    {
        await using var database = Database();
        var difficulties = await database.CreatureTemplateDifficulty.Where(x => x.PickpocketLootId == lootId).ToListAsync();
        return (Array.Empty<ICreatureTemplate>(), difficulties);
    }
    
    public override async Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId)
    {
        await using var model = Database();
        return await model.WaypointData.Where(wp => wp.PathId == pathId).OrderBy(wp => wp.PointId).ToListAsync<IWaypointData>();
    }

    public override async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(int sourceType, int sourceEntry, int sourceId)
    {
        await using var model = Database();

        return await model.ConditionsMaster.Where(line =>
                line.SourceType == sourceType && line.SourceEntry == sourceEntry && line.SourceId == sourceId)
            .ToListAsync<IConditionLine>();
    }

    public override async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key)
    {
        await using var model = Database();

        return await model.ConditionsMaster.Where(x => x.SourceType == key.SourceType &&
                                                       (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceGroup) || x.SourceGroup == (key.SourceGroup ?? 0)) &&
                                                       (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceEntry)  || x.SourceEntry == (key.SourceEntry ?? 0)) &&
                                                       (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceId)  || x.SourceId == (key.SourceId ?? 0)))
            .ToListAsync<IConditionLine>();
    }

    public override async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys)
    {
        if (manualKeys.Count == 0)
            return new List<IConditionLine>();
            
        await using var model = Database();
        var predicate = PredicateBuilder.New<MySqlConditionLineMaster>();
        foreach (var key in manualKeys)
        {
            predicate = predicate.Or(x => x.SourceType == key.SourceType &&
                                          (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceGroup) || x.SourceGroup == (key.SourceGroup ?? 0)) &&
                                          (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceEntry)  || x.SourceEntry == (key.SourceEntry ?? 0)) &&
                                          (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceId)  || x.SourceId == (key.SourceId ?? 0)));
        }
        return await model.ConditionsMaster.Where(predicate).ToListAsync<IConditionLine>();
    }

    public override async Task<IReadOnlyList<IConversationActor>> GetConversationActors()
    {
        await using var model = Database();
        return await model.ConversationActor.OrderBy(x => x.ConversationId).ThenBy(x => x.Idx).ToListAsync<IConversationActor>();
    }
}