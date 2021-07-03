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
using WDE.SkyFireMySqlDatabase.Models;
using WDE.SkyFireMySqlDatabase.Providers;

namespace WDE.SkyFireMySqlDatabase.Database
{
    public class SkyFireMySqlDatabaseProvider : IAsyncDatabaseProvider, IAuthDatabaseProvider
    {
        public bool IsConnected => true;

        private readonly ICurrentCoreVersion currentCoreVersion;

        public SkyFireMySqlDatabaseProvider(IWorldDatabaseSettingsProvider settings,
            IAuthDatabaseSettingsProvider authSettings,
            DatabaseLogger databaseLogger,
            ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
            DataConnection.TurnTraceSwitchOn();
            DataConnection.WriteTraceLine = databaseLogger.Log;
            DataConnection.DefaultSettings = new MySqlSettings(settings.Settings, authSettings.Settings);
        }
        
        public ICreatureTemplate? GetCreatureTemplate(uint entry)
        {
            using var model = new SkyFireDatabase();
            return model.CreatureTemplate.FirstOrDefault(ct => ct.Entry == entry);
        }

        public IEnumerable<ICreatureTemplate> GetCreatureTemplates()
        {
            using var model = new SkyFireDatabase();
            return model.CreatureTemplate.OrderBy(t => t.Entry).ToList<ICreatureTemplate>();
        }
        
        public async Task<List<ICreatureTemplate>> GetCreatureTemplatesAsync()
        {
            await using var model = new SkyFireDatabase();
            return await model.CreatureTemplate.OrderBy(t => t.Entry).ToListAsync<ICreatureTemplate>();
        }

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            using var model = new SkyFireDatabase();
            return model.SmartScript.Where(line => line.EntryOrGuid == entryOrGuid && line.ScriptSourceType == (int) type).ToList();
        }

        public IEnumerable<IGameEvent> GetGameEvents()
        {
            using var model = new SkyFireDatabase();
            return (from t in model.GameEvents orderby t.Entry select t).ToList<IGameEvent>();
        }
        
        public async Task<List<IGameEvent>> GetGameEventsAsync()
        {
            await using var model = new SkyFireDatabase();
            return await (from t in model.GameEvents orderby t.Entry select t).ToListAsync<IGameEvent>();
        }
        
        public IEnumerable<IConversationTemplate> GetConversationTemplates()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IConversationTemplate)))
                return new List<IConversationTemplate>();
            
            using var model = new SkyFireDatabase();
            return (from t in model.ConversationTemplate orderby t.Id select t).ToList<IConversationTemplate>();
        }
        
        public async Task<List<IConversationTemplate>> GetConversationTemplatesAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IConversationTemplate)))
                return new List<IConversationTemplate>();
            
            await using var model = new SkyFireDatabase();
            return await (from t in model.ConversationTemplate orderby t.Id select t).ToListAsync<IConversationTemplate>();
        }
        
        public IEnumerable<IGossipMenu> GetGossipMenus()
        {
            using var model = new SkyFireDatabase();

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
            await using var model = new SkyFireDatabase();

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

        public IEnumerable<INpcText> GetNpcTexts()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
                return new List<INpcText>();
            
            using var model = new SkyFireDatabase();
            return (from t in model.NpcTexts orderby t.Id select t).ToList<INpcText>();
        }

        public async Task<List<INpcText>> GetNpcTextsAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
                return new List<INpcText>();
            
            await using var model = new SkyFireDatabase();
            return await (from t in model.NpcTexts orderby t.Id select t).ToListAsync<INpcText>();
        }

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates()
        {
            var task = GetAreaTriggerTemplatesAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task<List<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(ICreatureClassLevelStat)))
                return new List<ICreatureClassLevelStat>();

            await using var model = new SkyFireDatabase();
            return await (from t in model.CreatureClassLevelStats select t).ToListAsync<ICreatureClassLevelStat>();
        }
        
        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(ICreatureClassLevelStat)))
                return Enumerable.Empty<ICreatureClassLevelStat>();

            using var model = new SkyFireDatabase();
            return (from t in model.CreatureClassLevelStats select t).ToList<ICreatureClassLevelStat>();
        }

        public async Task<List<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IAreaTriggerTemplate)))
                return new List<IAreaTriggerTemplate>();

            await using var model = new SkyFireDatabase();
            return await (from t in model.AreaTriggerTemplate orderby t.Id select t).ToListAsync<IAreaTriggerTemplate>();
        }
        
        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates()
        {
            using var model = new SkyFireDatabase();
            return (from t in model.GameObjectTemplate orderby t.Entry select t).ToList<IGameObjectTemplate>();
        }
        
        public async Task<List<IGameObjectTemplate>> GetGameObjectTemplatesAsync()
        {
            await using var model = new SkyFireDatabase();
            return await (from t in model.GameObjectTemplate orderby t.Entry select t).ToListAsync<IGameObjectTemplate>();
        }
        
        private IQueryable<MySqlQuestTemplate> GetQuestsQuery(SkyFireDatabase model)
        {
            return (from t in model.QuestTemplate select t);
        }
        
        public IEnumerable<IQuestTemplate> GetQuestTemplates()
        {
            using var model = new SkyFireDatabase();

            return GetQuestsQuery(model).ToList<IQuestTemplate>();
        }

        public async Task<List<IQuestTemplate>> GetQuestTemplatesAsync()
        {
            await using var model = new SkyFireDatabase();
            return await GetQuestsQuery(model).ToListAsync<IQuestTemplate>();
        }

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
            using var model = new SkyFireDatabase();
            return model.GameObjectTemplate.FirstOrDefault(g => g.Entry == entry);
        }

        public IQuestTemplate? GetQuestTemplate(uint entry)
        {
            using var model = new SkyFireDatabase();
            return model.QuestTemplate.FirstOrDefault(q => q.Entry == entry);
        }

        public async Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script)
        {
            using var writeLock = await DatabaseLock.WriteLock();
            await using var model = new SkyFireDatabase();

            await model.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            await model.SmartScript.Where(x => x.EntryOrGuid == entryOrGuid && x.ScriptSourceType == (int) type).DeleteAsync();

            switch (type)
            {
                case SmartScriptType.Creature:
                {
                    uint entry = 0;
                    if (entryOrGuid < 0)
                    {
                        var template = await model.Creature.Where(p => p.Guid == (uint)-entryOrGuid).FirstOrDefaultAsync();
                        if (template == null)
                            throw new Exception(
                                $"Trying to install creature script for guid {-entryOrGuid}, but this guid doesn't exist in creature table, so entry cannot be determined.");
                        entry = template.Entry;
                    }
                    await model.CreatureTemplate.Where(p => p.Entry == entry)
                        .Set(p => p.AIName, currentCoreVersion.Current.SmartScriptFeatures.CreatureSmartAiName)
                        .Set(p => p.ScriptName, "")
                        .UpdateAsync();
                    break;
                }
                case SmartScriptType.GameObject:
                {
                    uint entry = 0;
                    if (entryOrGuid < 0)
                    {
                        var template = await model.GameObject.Where(p => p.Guid == (uint)-entryOrGuid).FirstOrDefaultAsync();
                        if (template == null)
                            throw new Exception(
                                $"Trying to install gameobject script for guid {-entryOrGuid}, but this guid doesn't exist in gameobject table, so entry cannot be determined.");
                        entry = template.Entry;
                    }
                    await model.GameObjectTemplate.Where(p => p.Entry == entry)
                        .Set(p => p.AIName, currentCoreVersion.Current.SmartScriptFeatures.GameObjectSmartAiName)
                        .Set(p => p.ScriptName, "")
                        .UpdateAsync();
                    break;
                }
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

        public async Task InstallConditions(IEnumerable<IConditionLine> conditionLines,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null)
        {
            using var writeLock = await DatabaseLock.WriteLock();
            await using var model = new SkyFireDatabase();

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
            using var model = new SkyFireDatabase();

            return model.Conditions.Where(line =>
                    line.SourceType == sourceType && line.SourceEntry == sourceEntry && line.SourceId == sourceId)
                .ToList();
        }

        public async Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key)
        {
            await using var model = new SkyFireDatabase();

            return await model.Conditions.Where(x => x.SourceType == key.SourceType &&
                                                     (!keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceGroup) || x.SourceGroup == (key.SourceGroup ?? 0)) &&
                                                     (!keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceEntry)  || x.SourceEntry == (key.SourceEntry ?? 0)) &&
                                                     (!keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceId)  || x.SourceId == (key.SourceId ?? 0)))
                .ToListAsync<IConditionLine>();
        }

        public async Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType)
        {
            var type = (int)scriptType;
            await using var model = new SkyFireDatabase();
            return await model.SmartScript
                .Where(t => t.ScriptSourceType == type)
                .GroupBy(t => t.EntryOrGuid, t => t.EntryOrGuid)
                .Select(t => t.Key)
                .ToListAsync();
        }
        
        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId)
        {
            using var model = new SkyFireDatabase();
            return model.SpellScriptNames.Where(spell => spell.SpellId == spellId).ToList();
        }
        
        public async Task<List<IBroadcastText>> GetBroadcastTextsAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IBroadcastText)))
                return await Task.FromResult(new List<IBroadcastText>());
            
            await using var model = new SkyFireDatabase();
            return await (from t in model.BroadcastTexts select t).ToListAsync<IBroadcastText>();
        }
        
        public IBroadcastText? GetBroadcastTextByText(string text)
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IBroadcastText)))
                return null;
            
            using var model = new SkyFireDatabase();
            return (from t in model.BroadcastTexts where t.Text == text || t.Text1 == text select t).FirstOrDefault();
        }

        public ICreature? GetCreatureByGuid(uint guid)
        {
            using var model = new SkyFireDatabase();
            return model.Creature.FirstOrDefault(c => c.Guid == guid);
        }

        public IGameObject? GetGameObjectByGuid(uint guid)
        {
            using var model = new SkyFireDatabase();
            return model.GameObject.FirstOrDefault(g => g.Guid == guid);
        }

        public IEnumerable<ISmartScriptProjectItem> GetProjectItems() => Enumerable.Empty<ISmartScriptProjectItem>();
        public IEnumerable<ISmartScriptProject> GetProjects() => Enumerable.Empty<ISmartScriptProject>();
        
        public async Task<IList<IAuthRbacPermission>> GetRbacPermissionsAsync()
        {
            if (!Supports<IAuthRbacPermission>())
                return new List<IAuthRbacPermission>();
            
            await using var model = new SkyFireAuthDatabase();
            return await model.RbacPermissions.ToListAsync<IAuthRbacPermission>();
        }

        public async Task<IList<IAuthRbacLinkedPermission>> GetLinkedPermissionsAsync()
        {
            if (!Supports<IAuthRbacLinkedPermission>())
                return new List<IAuthRbacLinkedPermission>();
            
            await using var model = new SkyFireAuthDatabase();
            return await model.RbacLinkedPermissions.ToListAsync<IAuthRbacLinkedPermission>();
        }

        private bool Supports<T>()
        {
            return !currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(T));
        }
    }
}