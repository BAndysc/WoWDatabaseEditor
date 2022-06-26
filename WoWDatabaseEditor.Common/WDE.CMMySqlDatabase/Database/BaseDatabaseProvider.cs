using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LinqKit;
using LinqToDB;
using LinqToDB.Data;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.MySqlDatabaseCommon.CommonModels;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.CMMySqlDatabase.Models;

namespace WDE.CMMySqlDatabase.Database
{
    public abstract class BaseDatabaseProvider<T> : IAsyncDatabaseProvider, IAuthDatabaseProvider where T : BaseDatabaseTables, new()
    {
        public bool IsConnected => true;
        public abstract ICreatureTemplate? GetCreatureTemplate(uint entry);
        public abstract IEnumerable<ICreatureTemplate> GetCreatureTemplates();

        private readonly ICurrentCoreVersion currentCoreVersion;

        public BaseDatabaseProvider(IWorldDatabaseSettingsProvider settings,
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
        
        public async Task<List<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry)
        {
            await using var model = Database();
            return await model.CreatureTexts.Where(t => t.CreatureId == entry)
                .OrderBy(t => t.GroupId)
                .ThenBy(t => t.Id)
                .ToListAsync<ICreatureText>();
        }

        public IReadOnlyList<ICreatureText>? GetCreatureTextsByEntry(uint entry)
        {
            using var model = Database();
            return model.CreatureTexts.Where(t => t.CreatureId == entry)
                .OrderBy(t => t.GroupId)
                .ThenBy(t => t.Id)
                .ToList<ICreatureText>();
        }
        
        public async Task<IList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList)
        {
            return new List<ISmartScriptLine>();
        }

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            return new List<ISmartScriptLine>();
        }

        public async Task<IQuestRequestItem?> GetQuestRequestItem(uint entry)
        {
            await using var model = Database();
            return await model.QuestRequestItems.FirstOrDefaultAsync<IQuestRequestItem>(quest => quest.Entry == entry);
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
            return new List<IConversationTemplate>();
        }

        public abstract Task<List<ICreatureTemplate>> GetCreatureTemplatesAsync();
        public abstract Task<List<ICreature>> GetCreaturesAsync();

        public async Task<List<IConversationTemplate>> GetConversationTemplatesAsync()
        {
            return new List<IConversationTemplate>();
        }
        
        public IEnumerable<IGossipMenu> GetGossipMenus()
        {
            using var model = Database();

            List<GossipMenuLineWoTLK> gossips;
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
                .Select(t => new GossipMenuWoTLK(t.Key, t.Where(t => t.Text != null).Select(t => t.Text!).ToList()))
                .ToList<IGossipMenu>();
        }
        
        public async Task<List<IGossipMenu>> GetGossipMenusAsync()
        {
            await using var model = Database();

            List<GossipMenuLineWoTLK> gossips;
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
                .Select(t => new GossipMenuWoTLK(t.Key, t.Where(t => t.Text != null).Select(t => t.Text!).ToList()))
                .ToList<IGossipMenu>();
        }

        public abstract Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId);
        public abstract List<IGossipMenuOption> GetGossipMenuOptions(uint menuId);

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
        
        public async Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry)
        {
            return null;
        }

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates()
        {
            return new List<IAreaTriggerTemplate>();
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
            return new List<IAreaTriggerTemplate>();
        }
        
        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates()
        {
            using var model = Database();
            return (from t in model.GameObjectTemplate orderby t.Entry select t).ToList<IGameObjectTemplate>();
        }
        
        public async Task<List<IGameObjectTemplate>> GetGameObjectTemplatesAsync()
        {
            await using var model = Database();
            var o = from t in model.GameObjectTemplate orderby t.Entry select t;
            return await (o).ToListAsync<IGameObjectTemplate>();
        }

        public abstract Task<List<IGameObject>> GetGameObjectsAsync();

        protected virtual IQueryable<QuestTemplateWoTLK> GetQuestsQuery(BaseDatabaseTables model)
        {
            return from t in model.QuestTemplate
                orderby t.Entry
                select t;
        }
        
        public virtual IEnumerable<IQuestTemplate> GetQuestTemplates()
        {
            using var model = Database();

            return GetQuestsQuery(model).ToList<IQuestTemplate>();
        }

        public virtual async Task<List<IQuestTemplate>> GetQuestTemplatesAsync()
        {
            await using var model = Database();
            var o = GetQuestsQuery(model);
            return await o.ToListAsync<IQuestTemplate>();
        }

        public virtual IQuestTemplate? GetQuestTemplate(uint entry)
        {
            using var model = Database();
            return model.QuestTemplate.FirstOrDefault(q => q.Entry == entry);
        }

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
            using var model = Database();
            return model.GameObjectTemplate.FirstOrDefault(g => g.Entry == entry);
        }

        public async Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IList<ISmartScriptLine> script)
        {
        }

        protected abstract Task<ICreature?> GetCreatureByGuid(T model, uint guid);
        protected abstract Task<IGameObject?> GetGameObjectByGuidAsync(T model, uint guid);
        protected abstract Task SetCreatureTemplateAI(T model, uint entry, string ainame, string scriptname);

        public async Task InstallConditions(IEnumerable<IConditionLine> conditionLines,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null)
        {
        }

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId)
        {
            return new List<IConditionLine>();
        }

        public async Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key)
        {
            return new List<IConditionLine>();
        }

        public async Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType)
        {
            return new List<int>();
        }
        
        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId)
        {
            using var model = Database();
            return model.SpellScriptNames.Where(spell => spell.SpellId == spellId).ToList();
        }

        public abstract ICreature? GetCreatureByGuid(uint guid);

        public abstract IGameObject? GetGameObjectByGuid(uint guid);

        public abstract IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry);
        public abstract Task<IList<ICreature>> GetCreaturesByEntryAsync(uint entry);
        public abstract Task<IList<IGameObject>> GetGameObjectsByEntryAsync(uint entry);

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

        public async Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions)
        {
            return new List<ISmartScriptLine>();
        }
        
        public virtual Task<IList<IItem>?> GetItemTemplatesAsync() => Task.FromResult<IList<IItem>?>(null);

        private ExpressionStarter<T> GenerateWhereConditionsForEventScript<T>(IEnumerable<(uint command, int dataIndex, long valueToSearch)> conditions) where T : IEventScriptLine
        {
            var predicate = PredicateBuilder.New<T>();
            foreach (var value in conditions)
            {
                if (value.dataIndex == 0)
                    predicate = predicate.Or(o => o.Command == value.command && o.DataLong1 == (ulong)value.valueToSearch);
                else if (value.dataIndex == 1)
                    predicate = predicate.Or(o => o.Command == value.command && o.DataLong2 == (ulong)value.valueToSearch);
                else if (value.dataIndex == 2)
                    predicate = predicate.Or(o => o.Command == value.command && o.DataInt == (int)value.valueToSearch);
            }
            return predicate;
        }

        public async Task<List<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions)
        {
            await using var model = Database();
            var events = await model.EventScripts.Where(GenerateWhereConditionsForEventScript<MySqlEventScriptLine>(conditions)).ToListAsync<IEventScriptLine>();
            var spells = await model.SpellScripts.Where(GenerateWhereConditionsForEventScript<MySqlSpellScriptLine>(conditions)).ToListAsync<IEventScriptLine>();
            var waypoints = await model.WaypointScripts.Where(GenerateWhereConditionsForEventScript<MySqlWaypointScriptLine>(conditions)).ToListAsync<IEventScriptLine>();
            return events.Concat(spells).Concat(waypoints).ToList();
        }

        public abstract Task<IList<ICreatureModelInfo>> GetCreatureModelInfoAsync();

        public abstract ICreatureModelInfo? GetCreatureModelInfo(uint displayId);

        public async Task<List<IEventScriptLine>> GetEventScript(EventScriptType type, uint id)
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public ISceneTemplate? GetSceneTemplate(uint sceneId)
        {
            if (!Supports<ISceneTemplate>())
                return null;
            using var model = Database();
            return model.SceneTemplates.FirstOrDefault(x => x.SceneId == sceneId);
        }
        
        public async Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId)
        {
            if (!Supports<ISceneTemplate>())
                return null;
            await using var model = Database();
            return await model.SceneTemplates.FirstOrDefaultAsync(x => x.SceneId == sceneId);
        }

        public async Task<IList<ISceneTemplate>?> GetSceneTemplatesAsync()
        {
            if (!Supports<ISceneTemplate>())
                return null; 
            await using var model = Database();
            return await model.SceneTemplates.ToListAsync<ISceneTemplate>();
        }

        public async Task<IList<IAuthRbacPermission>> GetRbacPermissionsAsync()
        {
            return new List<IAuthRbacPermission>();
        }

        public async Task<IList<IAuthRbacLinkedPermission>> GetLinkedPermissionsAsync()
        {
            return new List<IAuthRbacLinkedPermission>();
        }

        private bool Supports<R>()
        {
            return !currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(R));
        }
    }
}
