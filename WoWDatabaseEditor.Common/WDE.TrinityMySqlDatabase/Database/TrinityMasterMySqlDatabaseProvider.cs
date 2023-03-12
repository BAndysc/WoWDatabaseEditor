using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.TrinityMySqlDatabase.Models;

namespace WDE.TrinityMySqlDatabase.Database;

public class TrinityMasterMySqlDatabaseProvider : BaseTrinityMySqlDatabaseProvider<TrinityMasterDatabase>
{
    public TrinityMasterMySqlDatabaseProvider(IWorldDatabaseSettingsProvider settings, IAuthDatabaseSettingsProvider authSettings, DatabaseLogger databaseLogger, ICurrentCoreVersion currentCoreVersion) : base(settings, authSettings, databaseLogger, currentCoreVersion)
    {
    }

    public override ICreatureTemplate? GetCreatureTemplate(uint entry)
    {
        using var model = Database();
        var template = model.CreatureTemplate.FirstOrDefault(ct => ct.Entry == entry);
        if (template == null)
            return null;
        var models = model.CreatureTemplateModel.Where(x => x.CreatureId == entry).ToList();
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

    public override async Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId)
    {
        await using var model = Database();
        return await model.GossipMenuOptionsMaster.Where(option => option.MenuId == menuId).ToListAsync<IGossipMenuOption>();
    }
    
    public override List<IGossipMenuOption> GetGossipMenuOptions(uint menuId)
    {
        using var model = Database();
        return model.GossipMenuOptionsMaster.Where(option => option.MenuId == menuId).ToList<IGossipMenuOption>();
    }
    
    public override async Task<List<IBroadcastText>> GetBroadcastTextsAsync()
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

    public override ICreature? GetCreatureByGuid(uint guid)
    {
        using var model = Database();
        return model.Creature.FirstOrDefault(c => c.Guid == guid);
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

    public override async Task<IList<ICreature>> GetCreaturesByEntryAsync(uint entry)
    {
        await using var model = Database();
        return await model.Creature.Where(g => g.Entry == entry).ToListAsync<ICreature>();
    }

    public override async Task<IList<IGameObject>> GetGameObjectsByEntryAsync(uint entry)
    {
        await using var model = Database();
        return await model.GameObject.Where(g => g.Entry == entry).ToListAsync<IGameObject>();
    }

    public override async Task<IList<ICreature>> GetCreaturesAsync()
    {
        await using var model = Database();
        return await model.Creature.OrderBy(t => t.Entry).ToListAsync<ICreature>();
    }

    public override async Task<IList<ITrinityString>> GetStringsAsync()
    {
        await using var model = Database();
        return await model.Strings.ToListAsync<ITrinityString>();
    }

    public override async Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync()
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

    protected override async Task<ICreature?> GetCreatureByGuid(TrinityMasterDatabase model, uint guid)
    {
        return await model.Creature.FirstOrDefaultAsync(e => e.Guid == guid);
    }
    
    public override async Task<IList<IGameObject>> GetGameObjectsAsync()
    {
        await using var model = Database();
        return await model.GameObject.ToListAsync<IGameObject>();
    }

    public override IEnumerable<IGameObject> GetGameObjects()
    {
        using var model = Database();
        return model.GameObject.ToList<IGameObject>();
    }
    
    public override IGameObject? GetGameObjectByGuid(uint guid)
    {
        using var model = Database();
        return model.GameObject.FirstOrDefault(g => g.Guid == guid);
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
    
    public override async Task<IList<ICreature>> GetCreaturesByMapAsync(uint map)
    {
        await using var model = Database();
        return await model.Creature.Where(c => c.Map == map).ToListAsync<ICreature>();
    }

    public override async Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map)
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
        
    public override IReadOnlyList<IQuestTemplate> GetQuestTemplates()
    {
        using var model = Database();

        return GetQuestsQuery(model).ToList<IQuestTemplate>();
    }

    public override async Task<List<IQuestTemplate>> GetQuestTemplatesAsync()
    {
        await using var model = Database();
        return await GetQuestsQuery(model).ToListAsync<IQuestTemplate>();
    }

    public override IQuestTemplate? GetQuestTemplate(uint entry)
    {
        using var model = Database();
        var addon = model.QuestTemplateAddon.FirstOrDefault(addon => addon.Entry == entry);
        return model.MasterQuestTemplate.FirstOrDefault(q => q.Entry == entry)?.SetAddon(addon);
    }
    
    public override async Task<IList<ICreatureModelInfo>> GetCreatureModelInfoAsync()
    {
        await using var model = Database();
        return await model.CreatureModelInfo.ToListAsync<ICreatureModelInfo>();
    }

    public override ICreatureModelInfo? GetCreatureModelInfo(uint displayId)
    {
        using var model = Database();
        return model.CreatureModelInfo.FirstOrDefault(x => x.DisplayId == displayId);
    }

    public override async Task<IGameObject?> GetGameObjectByGuidAsync(uint guid)
    {
        await using var model = Database();
        return await model.GameObject.FirstOrDefaultAsync(x => x.Guid == guid);
    }

    public override async Task<ICreature?> GetCreaturesByGuidAsync(uint guid)
    {
        await using var model = Database();
        return await model.Creature.FirstOrDefaultAsync(x => x.Guid == guid);
    }
    
    
    public override async Task<IList<ICreatureAddon>> GetCreatureAddons()
    {
        await using var model = Database();
        return await model.CreatureAddon.ToListAsync<ICreatureAddon>();
    }

    public override async Task<IList<ICreatureTemplateAddon>> GetCreatureTemplateAddons()
    {
        await using var model = Database();
        return await model.CreatureTemplateAddon.ToListAsync<ICreatureTemplateAddon>();
    }
        
    public override async Task<ICreatureAddon?> GetCreatureAddon(uint guid)
    {
        await using var model = Database();
        return await model.CreatureAddon.FirstOrDefaultAsync<ICreatureAddon>(x => x.Guid == guid);
    }

    public override async Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry)
    {
        await using var model = Database();
        return await model.CreatureTemplateAddon.FirstOrDefaultAsync<ICreatureTemplateAddon>(x => x.Entry == entry);
    }

    public override async Task<IList<IPlayerChoice>?> GetPlayerChoicesAsync()
    {
        await using var model = Database();
        return await model.PlayerChoice.ToListAsync<IPlayerChoice>();
    }

    public override async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync()
    {
        await using var model = Database();
        return await model.PlayerChoiceResponse.ToListAsync<IPlayerChoiceResponse>();
    }

    public override async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId)
    {
        await using var model = Database();
        return await model.PlayerChoiceResponse.Where(x => x.ChoiceId == choiceId).ToListAsync<IPlayerChoiceResponse>();
    }

    public override async Task<IList<IQuestObjective>> GetQuestObjectives(uint questId)
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

    public override async Task<IList<IPhaseName>?> GetPhaseNamesAsync()
    {
        await using var model = Database();
        return await model.PhaseNames.ToListAsync<IPhaseName>();
    }

    public override IList<IPhaseName>? GetPhaseNames()
    {
        using var model = Database();
        return model.PhaseNames.ToList<IPhaseName>();
    }
}