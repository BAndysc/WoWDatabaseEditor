using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.CMMySqlDatabase.Models;

namespace WDE.CMMySqlDatabase.Database;

public class DatabaseProviderWoTLK : BaseDatabaseProvider<WoTLKDatabase>
{
    public DatabaseProviderWoTLK(IWorldDatabaseSettingsProvider settings, IAuthDatabaseSettingsProvider authSettings, DatabaseLogger databaseLogger, ICurrentCoreVersion currentCoreVersion) : base(settings, authSettings, databaseLogger, currentCoreVersion)
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
        return await (from t in model.BroadcastTexts orderby t._Id select t).ToListAsync<IBroadcastText>();
    }
    
    public override IBroadcastText? GetBroadcastTextByText(string text)
    {
        using var model = Database();
        return (from t in model.BroadcastTexts where t.Text == text || t.Text1 == text select t).FirstOrDefault();
    }

    public override async Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text)
    {
        await using var model = Database();
        return await (from t in model.BroadcastTexts where t.Text == text || t.Text1 == text select t).FirstOrDefaultAsync();
    }
    
    public override async Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id)
    {
        await using var model = Database();
        return await (from t in model.BroadcastTexts where t._Id == (int)id select t).FirstOrDefaultAsync();
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
    
    protected override async Task SetCreatureTemplateAI(WoTLKDatabase model, uint entry, string ainame, string scriptname)
    {
        await model.CreatureTemplate.Where(p => p.Entry == entry)
            .Set(p => p.AIName, ainame)
            .Set(p => p.ScriptName, scriptname)
            .UpdateAsync();
    }

    protected override async Task<ICreature?> GetCreatureByGuidAsync(WoTLKDatabase model, uint guid)
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
    
    protected override Task<IGameObject?> GetGameObjectByGuidAsync(WoTLKDatabase model, uint guid)
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

    public override async Task<IReadOnlyList<IItem>?> GetItemTemplatesAsync()
    {
        await using var model = Database();
        return await model.ItemTemplate.ToListAsync<IItem>();
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
    
    public override IReadOnlyList<IGameObjectTemplate> GetGameObjectTemplates()
    {
        using var model = Database();
        return (from t in model.GameObjectTemplate orderby t.Entry select t).ToList<IGameObjectTemplate>();
    }
        
    public override async Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectTemplatesAsync()
    {
        await using var model = Database();
        var o = from t in model.GameObjectTemplate orderby t.Entry select t;
        return await (o).ToListAsync<IGameObjectTemplate>();
    }
        
    public override async Task<IGameObjectTemplate?> GetGameObjectTemplate(uint entry)
    {
        await using var model = Database();
        return await model.GameObjectTemplate.FirstOrDefaultAsync(g => g.Entry == entry);
    }
    
    public override async Task<IReadOnlyList<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync()
    {
        await using var model = Database();
        return await (from t in model.CreatureClassLevelStats select t).ToListAsync<ICreatureClassLevelStat>();
    }

    public override IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats()
    {
        using var model = Database();
        return (from t in model.CreatureClassLevelStats select t).ToList<ICreatureClassLevelStat>();
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
    
    public override async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureLootCrossReference(uint lootId)
    {
        await using var database = Database();
        var creatures = await database.CreatureTemplate.Where(x => x.LootId == lootId).ToListAsync();
        return (creatures, Array.Empty<ICreatureTemplateDifficulty>());
    }

    public override async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureSkinningLootCrossReference(uint lootId)
    {
        await using var database = Database();
        var creatures = await database.CreatureTemplate.Where(x => x.SkinningLootId == lootId).ToListAsync();
        return (creatures, Array.Empty<ICreatureTemplateDifficulty>());
    }

    public override async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreaturePickPocketLootCrossReference(uint lootId)
    {
        await using var database = Database();
        var creatures = await database.CreatureTemplate.Where(x => x.PickpocketLootId == lootId).ToListAsync();
        return (creatures, Array.Empty<ICreatureTemplateDifficulty>());
    }
    
    public override async Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectLootCrossReference(uint lootId)
    {
        await using var database = Database();
        return await database.GameObjectTemplate.Where(template =>
            template.Type == GameobjectType.Chest && template.Data1 == lootId ||
            template.Type == GameobjectType.FishingHole && template.Data1 == lootId).ToListAsync();
    }
}