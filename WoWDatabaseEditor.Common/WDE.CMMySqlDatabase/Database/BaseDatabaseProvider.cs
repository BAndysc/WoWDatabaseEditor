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
    public abstract class BaseDatabaseProvider<T> : IAsyncDatabaseProvider, IAuthDatabaseProvider, IMangosDatabaseProvider where T : BaseDatabaseTables, new()
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
            return new List<ICreatureText>();
        }

        public IReadOnlyList<ICreatureText>? GetCreatureTextsByEntry(uint entry)
        {
            return null;
        }
        
        public async Task<IList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList)
        {
            return new List<ISmartScriptLine>();
        }

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            return new List<ISmartScriptLine>();
        }

        public async Task<IList<IQuestObjective>> GetQuestObjectives(uint questId) => new List<IQuestObjective>();

        public async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => null;

        public async Task<IQuestRequestItem?> GetQuestRequestItem(uint entry)
        {
            return null;
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

        public abstract Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync();
        public abstract Task<IList<ICreature>> GetCreaturesAsync();

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

        public abstract Task<List<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync();

        public async Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry)
        {
            return null;
        }

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates()
        {
            return new List<IAreaTriggerTemplate>();
        }

        public abstract IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats();

        public abstract Task<List<IBroadcastText>> GetBroadcastTextsAsync();
        
        public async Task<IList<ISmartScriptLine>> GetScriptForAsync(int entryOrGuid, SmartScriptType type) => new List<ISmartScriptLine>();

        public async Task<List<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync()
        {
            return new List<IAreaTriggerTemplate>();
        }
        
        public abstract IEnumerable<IGameObjectTemplate> GetGameObjectTemplates();
        public abstract Task<List<IGameObjectTemplate>> GetGameObjectTemplatesAsync();
        public abstract IGameObjectTemplate? GetGameObjectTemplate(uint entry);
        public abstract Task<IList<IGameObject>> GetGameObjectsAsync();

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

        public async Task<IList<IPlayerChoice>?> GetPlayerChoicesAsync() => null;

        public async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => null;

        public async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => null;

        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId)
        {
            using var model = Database();
            return model.SpellScriptNames.Where(spell => spell.SpellId == spellId).ToList();
        }

        public async Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text)
        {
            await using var model = Database();
            return await model.BroadcastTextLocale.FirstOrDefaultAsync(b => b.Text == text || b.Text1 == text);
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

        public IEnumerable<ISmartScriptProjectItem> GetLegacyProjectItems() => Enumerable.Empty<ISmartScriptProjectItem>();
        public IEnumerable<ISmartScriptProject> GetLegacyProjects() => Enumerable.Empty<ISmartScriptProject>();
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

        public abstract Task<IList<ICreatureAddon>> GetCreatureAddons();
        public abstract Task<IList<ICreatureTemplateAddon>> GetCreatureTemplateAddons();

        public async Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates()
        {
            return null;
        }

        public async Task<IList<IGameEventCreature>> GetGameEventCreaturesAsync()
        {
            await using var model = Database();
            return await model.GameEventCreature.ToListAsync<IGameEventCreature>();
        }

        public async Task<IList<IGameEventGameObject>> GetGameEventGameObjectsAsync()
        {
            await using var model = Database();
            return await model.GameEventGameObject.ToListAsync<IGameEventGameObject>();
        }

        public async Task<IList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint guid)
        {
            await using var model = Database();
            return await model.GameEventCreature.Where(x => x.Guid == guid).ToListAsync<IGameEventCreature>();
        }

        public async Task<IList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint guid)
        {
            await using var model = Database();
            return await model.GameEventGameObject.Where(x => x.Guid == guid).ToListAsync<IGameEventGameObject>();
        }

        public async Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry)
        {
            return null;
        }

        public async Task<IList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates()
        {
            await using var model = Database();
            return await model.CreatureEquipmentTemplate.ToListAsync<IMangosCreatureEquipmentTemplate>();
        }

        public abstract Task<IGameObject?> GetGameObjectByGuidAsync(uint guid);

        public abstract Task<ICreature?> GetCreaturesByGuidAsync(uint guid);
        public abstract Task<ICreatureAddon?> GetCreatureAddon(uint guid);
        public abstract Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry);

        public async Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId) => null;

        public async Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId) => null;

        public async Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId) => null;

        public async Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId)
        {
            await using var model = Database();
            return await model.WaypointPath.Where(x => x.PathId == pathId).OrderBy(x => x.PointId).ToListAsync<IMangosWaypoint>();
        }

        public async Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid)
        {
            await using var model = Database();
            return await model.CreatureMovement.Where(x => x.Guid == guid).OrderBy(x => x.PointId).ToListAsync<IMangosCreatureMovement>();
        }

        public async Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId)
        {
            await using var model = Database();
            var predicate = PredicateBuilder.New<MangosCreatureMovementTemplate>(x => x.Entry == entry);
            if (pathId.HasValue)
                predicate = predicate.And(x => x.PathId == pathId);
            return await model.CreatureMovementTemplate.Where(predicate).OrderBy(x => x.PointId).ToListAsync<IMangosCreatureMovementTemplate>();
        }

        public async Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId)
        {
            await using var model = Database();
            return await model.WaypointPathName.FirstOrDefaultAsync(x => x.PathId == pathId);
        }
        
        public async Task<List<IEventAiLine>> GetEventAi(int entryOrGuid) 
        {
            await using var model = Database();
            return await model.CreatureAiScripts.Where(x => x.CreatureIdOrGuid == entryOrGuid).OrderBy(x => x.Id).ToListAsync<IEventAiLine>();
        }

        public async Task<IList<IAuthRbacPermission>> GetRbacPermissionsAsync()
        {
            return new List<IAuthRbacPermission>();
        }

        public async Task<IList<IAuthRbacLinkedPermission>> GetLinkedPermissionsAsync()
        {
            return new List<IAuthRbacLinkedPermission>();
        }
        
        public async Task<IList<IDbScriptRandomTemplate>?> GetScriptRandomTemplates(uint id, IMangosDatabaseProvider.RandomTemplateType type)
        {
            await using var model = Database();
            return await model.DbScriptRandomTemplates.Where(x => x.Id == id && x.Type == (int)type).ToListAsync<IDbScriptRandomTemplate>();
        }

        public async Task<ICreatureAiSummon?> GetCreatureAiSummon(uint entry)
        {
            await using var model = Database();
            return await model.CreatureAiSummons.FirstOrDefaultAsync(x => x.Id == entry);
        }

        private bool Supports<R>()
        {
            return !currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(R));
        }
        
        public async Task<IList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync()
        {
            await using var model = Database();
            return await model.SpawnGroupTemplate.ToListAsync<ISpawnGroupTemplate>();
        }
    
        public async Task<IList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync()
        {
            await using var model = Database();
            return await model.SpawnGroupSpawns.ToListAsync<ISpawnGroupSpawn>();
        }
    
        public async Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id)
        {
            await using var model = Database();
            return await model.SpawnGroupTemplate.FirstOrDefaultAsync<ISpawnGroupTemplate>(x => x.Id == id);
        }

        public async Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id)
        {
            await using var model = Database();
            return await model.SpawnGroupFormations.FirstOrDefaultAsync<ISpawnGroupFormation>(x => x.Id == id);
        }

        public async Task<IList<ISpawnGroupFormation>?> GetSpawnGroupFormations()
        {
            await using var model = Database();
            return await model.SpawnGroupFormations.ToListAsync<ISpawnGroupFormation>();
        }
    
        public async Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type)
        {
            await using var model = Database();
            return await model.SpawnGroupSpawns.InnerJoin(
                model.SpawnGroupTemplate, 
                (t1, t2) => t1.TemplateId == t2.Id && t2.Type == type,
                (t1, t2) => t1)
                .FirstOrDefaultAsync(t1 => t1.Guid == guid);
        }
    }
}
