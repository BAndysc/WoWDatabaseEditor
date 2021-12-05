using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.TrinityMySqlDatabase.Models;

namespace WDE.TrinityMySqlDatabase.Database
{
    public abstract class BaseTrinityMySqlDatabaseProvider<T> : IAsyncDatabaseProvider, IAuthDatabaseProvider where T : BaseTrinityDatabase, new()
    {
        public bool IsConnected => true;
        public abstract ICreatureTemplate? GetCreatureTemplate(uint entry);
        public abstract IEnumerable<ICreatureTemplate> GetCreatureTemplates();

        private readonly ICurrentCoreVersion currentCoreVersion;

        public BaseTrinityMySqlDatabaseProvider(IWorldDatabaseSettingsProvider settings,
            IAuthDatabaseSettingsProvider authSettings,
            DatabaseLogger databaseLogger,
            ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
            DataConnection.TurnTraceSwitchOn();
            DataConnection.WriteTraceLine = databaseLogger.Log;
            DataConnection.DefaultSettings = new MySqlWorldSettings(settings.Settings, authSettings.Settings);
        }

        protected T Database() => new();
        
        public async Task<List<ICreatureText>> GetCreatureTextsByEntry(uint entry)
        {
            await using var model = Database();
            return await model.CreatureTexts.Where(t => t.CreatureId == entry)
                .OrderBy(t => t.GroupId)
                .ThenBy(t => t.Id)
                .ToListAsync<ICreatureText>();
        }

        public async Task<IList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList)
        {
            await using var model = Database();
            return await model.SmartScript.Where(line =>
                    (line.ActionType == 80 && line.ActionParam1 == timedActionList) ||
                    (line.ActionType == 88 && timedActionList >= line.ActionParam1 && timedActionList <= line.ActionParam2) ||
                   (line.ActionType == 87 && (line.ActionParam1 == timedActionList || line.ActionParam2 == timedActionList || line.ActionParam3 == timedActionList || line.ActionParam4 == timedActionList || line.ActionParam5 == timedActionList || line.ActionParam6 == timedActionList)))
                .ToListAsync<ISmartScriptLine>();
        }

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            using var model = Database();
            return model.SmartScript.Where(line => line.EntryOrGuid == entryOrGuid && line.ScriptSourceType == (int) type).ToList();
        }

        public IEnumerable<IGameEvent> GetGameEvents()
        {
            using var model = Database();
            return (from t in model.GameEvents orderby t.Entry select t).ToList<IGameEvent>();
        }
        
        public async Task<List<IGameEvent>> GetGameEventsAsync()
        {
            await using var model = Database();
            return await (from t in model.GameEvents orderby t.Entry select t).ToListAsync<IGameEvent>();
        }
        
        public IEnumerable<IConversationTemplate> GetConversationTemplates()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IConversationTemplate)))
                return new List<IConversationTemplate>();
            
            using var model = Database();
            return (from t in model.ConversationTemplate orderby t.Id select t).ToList<IConversationTemplate>();
        }

        public abstract Task<List<ICreatureTemplate>> GetCreatureTemplatesAsync();
        public abstract Task<List<ICreature>> GetCreaturesAsync();

        public async Task<List<IConversationTemplate>> GetConversationTemplatesAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IConversationTemplate)))
                return new List<IConversationTemplate>();
            
            await using var model = Database();
            return await (from t in model.ConversationTemplate orderby t.Id select t).ToListAsync<IConversationTemplate>();
        }
        
        public IEnumerable<IGossipMenu> GetGossipMenus()
        {
            using var model = Database();

            List<MySqlGossipMenuLine> gossips;
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
            {
                gossips = model.GossipMenus.ToList();
            }
            else
            {
                gossips = (from gossip in model.GossipMenus
                    join p in model.NpcTexts on gossip.TextId equals p.Id into lj
                    from lp in lj.DefaultIfEmpty()
                    select gossip.SetText(lp)).ToList();   
            }

            return gossips.GroupBy(g => g.MenuId)
                .Select(t => new MySqlGossipMenu(t.Key, t.Where(t => t.Text != null).Select(t => t.Text!).ToList()))
                .ToList<IGossipMenu>();
        }
        
        public async Task<List<IGossipMenu>> GetGossipMenusAsync()
        {
            await using var model = Database();

            List<MySqlGossipMenuLine> gossips;
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
            {
                gossips = await model.GossipMenus.ToListAsync();
            }
            else
            {
                gossips = await (from gossip in model.GossipMenus
                    join p in model.NpcTexts on gossip.TextId equals p.Id into lj
                    from lp in lj.DefaultIfEmpty()
                    select gossip.SetText(lp)).ToListAsync();   
            }

            return gossips.GroupBy(g => g.MenuId)
                .Select(t => new MySqlGossipMenu(t.Key, t.Where(t => t.Text != null).Select(t => t.Text!).ToList()))
                .ToList<IGossipMenu>();
        }

        public abstract Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId);

        public INpcText? GetNpcText(uint entry)
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
                return null;
            
            using var model = Database();
            return model.NpcTexts.FirstOrDefault(text => text.Id == entry);
        }

        public IEnumerable<INpcText> GetNpcTexts()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
                return new List<INpcText>();
            
            using var model = Database();
            return (from t in model.NpcTexts orderby t.Id select t).ToList<INpcText>();
        }

        public async Task<List<INpcText>> GetNpcTextsAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
                return new List<INpcText>();
            
            await using var model = Database();
            return await (from t in model.NpcTexts orderby t.Id select t).ToListAsync<INpcText>();
        }

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IAreaTriggerTemplate)))
                return new List<IAreaTriggerTemplate>();

            using var model = Database();
            return (from t in model.AreaTriggerTemplate orderby t.Id select t).ToList<IAreaTriggerTemplate>();
        }
        
        public async Task<List<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(ICreatureClassLevelStat)))
                return new List<ICreatureClassLevelStat>();

            await using var model = Database();
            return await (from t in model.CreatureClassLevelStats select t).ToListAsync<ICreatureClassLevelStat>();
        }

        public abstract Task<List<IBroadcastText>> GetBroadcastTextsAsync();

        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(ICreatureClassLevelStat)))
                return Enumerable.Empty<ICreatureClassLevelStat>();

            using var model = Database();
            return (from t in model.CreatureClassLevelStats select t).ToList<ICreatureClassLevelStat>();
        }

        public async Task<List<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IAreaTriggerTemplate)))
                return new List<IAreaTriggerTemplate>();

            await using var model = Database();
            return await (from t in model.AreaTriggerTemplate orderby t.Id select t).ToListAsync<IAreaTriggerTemplate>();
        }
        
        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates()
        {
            using var model = Database();
            return (from t in model.GameObjectTemplate orderby t.Entry select t).ToList<IGameObjectTemplate>();
        }
        
        public async Task<List<IGameObjectTemplate>> GetGameObjectTemplatesAsync()
        {
            await using var model = Database();
            return await (from t in model.GameObjectTemplate orderby t.Entry select t).ToListAsync<IGameObjectTemplate>();
        }

        public abstract Task<List<IGameObject>> GetGameObjectsAsync();

        private IQueryable<MySqlQuestTemplate> GetQuestsQuery(BaseTrinityDatabase model)
        {
            return (from t in model.QuestTemplate
                join addon in model.QuestTemplateAddon on t.Entry equals addon.Entry into adn
                from subaddon in adn.DefaultIfEmpty()
                orderby t.Entry
                select t.SetAddon(subaddon));
        }
        
        public IEnumerable<IQuestTemplate> GetQuestTemplates()
        {
            using var model = Database();

            return GetQuestsQuery(model).ToList<IQuestTemplate>();
        }

        public async Task<List<IQuestTemplate>> GetQuestTemplatesAsync()
        {
            await using var model = Database();
            return await GetQuestsQuery(model).ToListAsync<IQuestTemplate>();
        }

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
            using var model = Database();
            return model.GameObjectTemplate.FirstOrDefault(g => g.Entry == entry);
        }

        public IQuestTemplate? GetQuestTemplate(uint entry)
        {
            using var model = Database();
            MySqlQuestTemplateAddon? addon = model.QuestTemplateAddon.FirstOrDefault(addon => addon.Entry == entry);
            return model.QuestTemplate.FirstOrDefault(q => q.Entry == entry)?.SetAddon(addon);
        }

        public async Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IList<ISmartScriptLine> script)
        {
            using var writeLock = await DatabaseLock.WriteLock();
            await using var model = Database();

            await model.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            
            foreach (var pair in script.Select(l => (l.ScriptSourceType, l.EntryOrGuid))
                .Concat(new (int ScriptSourceType, int EntryOrGuid)[]{((int)type, entryOrGuid)})
                .Distinct())
                await model.SmartScript.Where(x => x.EntryOrGuid == pair.EntryOrGuid && x.ScriptSourceType == pair.ScriptSourceType).DeleteAsync();

            switch (type)
            {
                case SmartScriptType.Creature:
                {
                    uint entry = 0;
                    if (entryOrGuid < 0)
                    {
                        var template = await GetCreatureByGuid(model, (uint)-entryOrGuid);
                        if (template == null)
                            throw new Exception(
                                $"Trying to install creature script for guid {-entryOrGuid}, but this guid doesn't exist in creature table, so entry cannot be determined.");
                        entry = template.Entry;
                    }
                    else
                        entry = (uint)entryOrGuid;

                    await SetCreatureTemplateAI(model, entry, currentCoreVersion.Current.SmartScriptFeatures.CreatureSmartAiName, "");
                    break;
                }
                case SmartScriptType.GameObject:
                {
                    uint entry = 0;
                    if (entryOrGuid < 0)
                    {
                        var template = await GetGameObjectByGuidAsync(model, (uint)-entryOrGuid);
                        if (template == null)
                            throw new Exception(
                                $"Trying to install gameobject script for guid {-entryOrGuid}, but this guid doesn't exist in gameobject table, so entry cannot be determined.");
                        entry = template.Entry;
                    }
                    else
                        entry = (uint)entryOrGuid;
                    await model.GameObjectTemplate.Where(p => p.Entry == entry)
                        .Set(p => p.AIName, currentCoreVersion.Current.SmartScriptFeatures.GameObjectSmartAiName)
                        .Set(p => p.ScriptName, "")
                        .UpdateAsync();
                    break;
                }
                case SmartScriptType.Quest:
                    var addonExists = await model.QuestTemplateAddon.Where(p => p.Entry == (uint)entryOrGuid).AnyAsync();
                    if (!addonExists)
                        await model.QuestTemplateAddon.InsertAsync(() => new MySqlQuestTemplateAddon()
                            {Entry = (uint)entryOrGuid});
                    await model.QuestTemplateAddonWithScriptName
                        .Where(p => p.Entry == (uint) entryOrGuid)
                        .Set(p => p.ScriptName, "SmartQuest")
                        .UpdateAsync();
                    break;
                case SmartScriptType.AreaTrigger:
                    await model.AreaTriggerScript.Where(p => p.Id == entryOrGuid).DeleteAsync();
                    await model.AreaTriggerScript.InsertAsync(() => new MySqlAreaTriggerScript(){Id = entryOrGuid, ScriptName = "SmartTrigger"});
                    break;
                case SmartScriptType.AreaTriggerEntity:
                    await model.AreaTriggerTemplate.Where(p => p.Id == (uint) entryOrGuid && p.IsServerSide == false)
                        .Set(p => p.ScriptName, "SmartAreaTriggerAI")
                        .UpdateAsync();
                    break;
                case SmartScriptType.AreaTriggerEntityServerSide:
                    await model.AreaTriggerTemplate.Where(p => p.Id == (uint) entryOrGuid && p.IsServerSide == true)
                        .Set(p => p.ScriptName, "SmartAreaTriggerAI")
                        .UpdateAsync();
                    break;
            }
            
            await model.SmartScript.BulkCopyAsync(script.Select(l => new MySqlSmartScriptLine(l)));

            await model.CommitTransactionAsync();
        }

        protected abstract Task<ICreature?> GetCreatureByGuid(T model, uint guid);
        protected abstract Task<IGameObject?> GetGameObjectByGuidAsync(T model, uint guid);
        protected abstract Task SetCreatureTemplateAI(T model, uint entry, string ainame, string scriptname);

        public async Task InstallConditions(IEnumerable<IConditionLine> conditionLines,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null)
        {
            using var writeLock = await DatabaseLock.WriteLock();
            await using var model = Database();

            var conditions = conditionLines?.ToList() ?? new List<IConditionLine>();
            List<(int SourceType, int? SourceGroup, int? SourceEntry, int? SourceId)> keys = conditions.Select(c =>
                    (c.SourceType, keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceGroup) ? (int?) c.SourceGroup : null,
                        keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceEntry) ? (int?) c.SourceEntry : null,
                        keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceId) ? (int?) c.SourceId : null))
                .Union(manualKey.HasValue
                    ? new[]
                    {
                        (manualKey.Value.SourceType, manualKey.Value.SourceGroup, manualKey.Value.SourceEntry,
                            manualKey.Value.SourceId)
                    }
                    : Array.Empty<(int, int?, int?, int?)>())
                .Distinct()
                .ToList();

            await model.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            foreach (var key in keys)
                await model.Conditions.Where(x => x.SourceType == key.SourceType &&
                                                  (!key.SourceGroup.HasValue || x.SourceGroup == key.SourceGroup.Value) &&
                                                  (!key.SourceEntry.HasValue || x.SourceEntry == key.SourceEntry.Value) &&
                                                  (!key.SourceId.HasValue || x.SourceId == key.SourceId.Value))
                    .DeleteAsync();

            if (conditions.Count > 0)
                await model.Conditions.BulkCopyAsync(conditions.Select(line => new MySqlConditionLine(line)));

            await model.CommitTransactionAsync();
        }

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId)
        {
            using var model = Database();

            return model.Conditions.Where(line =>
                    line.SourceType == sourceType && line.SourceEntry == sourceEntry && line.SourceId == sourceId)
                .ToList();
        }

        public async Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key)
        {
            await using var model = Database();

            return await model.Conditions.Where(x => x.SourceType == key.SourceType &&
                                                     (!keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceGroup) || x.SourceGroup == (key.SourceGroup ?? 0)) &&
                                                     (!keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceEntry)  || x.SourceEntry == (key.SourceEntry ?? 0)) &&
                                                     (!keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceId)  || x.SourceId == (key.SourceId ?? 0)))
                .ToListAsync<IConditionLine>();
        }

        public async Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType)
        {
            var type = (int)scriptType;
            await using var model = Database();
            return await model.SmartScript
                .Where(t => t.ScriptSourceType == type)
                .GroupBy(t => t.EntryOrGuid, t => t.EntryOrGuid)
                .Select(t => t.Key)
                .ToListAsync();
        }
        
        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId)
        {
            using var model = Database();
            return model.SpellScriptNames.Where(spell => spell.SpellId == spellId).ToList();
        }

        public abstract ICreature? GetCreatureByGuid(uint guid);

        public abstract IGameObject? GetGameObjectByGuid(uint guid);

        public abstract IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry);
        
        public abstract IEnumerable<ICreature> GetCreaturesByEntry(uint entry);


        public abstract IEnumerable<ICreature> GetCreatures();
        public abstract Task<IList<ICreature>> GetCreaturesByMapAsync(uint map);
        public abstract Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map);
        public abstract IEnumerable<IGameObject> GetGameObjects();

        public IEnumerable<ICoreCommandHelp> GetCommands()
        {
            using var model = Database();
            return model.Commands.ToList();
        }

        public abstract Task<IList<ITrinityString>> GetStringsAsync();
        public abstract Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync();

        public async Task<List<IPointOfInterest>> GetPointsOfInterestsAsync()
        {
            if (!Supports<IPointOfInterest>())
                return new List<IPointOfInterest>();
            
            await using var model = Database();
            return await model.PointsOfInterest.OrderBy(t => t.Id).ToListAsync<IPointOfInterest>();
        }

        public IEnumerable<ISmartScriptProjectItem> GetProjectItems() => Enumerable.Empty<ISmartScriptProjectItem>();
        public IEnumerable<ISmartScriptProject> GetProjects() => Enumerable.Empty<ISmartScriptProject>();
        public abstract IBroadcastText? GetBroadcastTextByText(string text);
        public abstract Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text);
        public abstract Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id);

        public async Task<IList<IAuthRbacPermission>> GetRbacPermissionsAsync()
        {
            if (!Supports<IAuthRbacPermission>())
                return new List<IAuthRbacPermission>();
            
            await using var model = new TrinityAuthDatabase();
            return await model.RbacPermissions.ToListAsync<IAuthRbacPermission>();
        }

        public async Task<IList<IAuthRbacLinkedPermission>> GetLinkedPermissionsAsync()
        {
            if (!Supports<IAuthRbacLinkedPermission>())
                return new List<IAuthRbacLinkedPermission>();
            
            await using var model = new TrinityAuthDatabase();
            return await model.RbacLinkedPermissions.ToListAsync<IAuthRbacLinkedPermission>();
        }

        private bool Supports<R>()
        {
            return !currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(R));
        }
    }
}
