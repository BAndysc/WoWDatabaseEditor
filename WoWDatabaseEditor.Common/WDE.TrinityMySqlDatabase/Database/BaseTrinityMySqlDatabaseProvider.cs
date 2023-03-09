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
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(IConversationTemplate)))
                return new List<IConversationTemplate>();
            
            using var model = Database();
            return (from t in model.ConversationTemplate orderby t.Id select t).ToList<IConversationTemplate>();
        }

        public abstract Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync();
        public abstract Task<IList<ICreature>> GetCreaturesAsync();

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

        public async Task<IGossipMenu?> GetGossipMenuAsync(uint menuId)
        {
            await using var model = Database();

            List<MySqlGossipMenuLine> gossips;
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
            {
                gossips = await model.GossipMenus.Where(g => g.MenuId == menuId).ToListAsync();
            }
            else
            {
                gossips = await (from gossip in model.GossipMenus
                    where gossip.MenuId == menuId
                    join p in model.NpcTexts on gossip.TextId equals p.Id into lj
                    from lp in lj.DefaultIfEmpty()
                    select gossip.SetText(lp)).ToListAsync();   
            }

            if (gossips.Count == 0)
                return null;

            return new MySqlGossipMenu(menuId, gossips.Where(t => t.Text != null).Select(t => t.Text!).ToList());
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
            await using var model = Database();
            return await model.AreaTriggerScript.FirstOrDefaultAsync(script => script.Entry == entry);
        }

        public virtual async Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry)
        {
            return null;
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
        
        public async Task<IList<ISmartScriptLine>> GetScriptForAsync(int entryOrGuid, SmartScriptType type)
        {
            await using var model = Database();
            return await model.SmartScript.Where(line => line.EntryOrGuid == entryOrGuid && line.ScriptSourceType == (int) type).ToListAsync<ISmartScriptLine>();
        }

        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(ICreatureClassLevelStat)))
                return Enumerable.Empty<ICreatureClassLevelStat>();

            using var model = Database();
            return (from t in model.CreatureClassLevelStats select t).ToList<ICreatureClassLevelStat>();
        }

        public async Task<IList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync()
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

        public abstract Task<IList<IGameObject>> GetGameObjectsAsync();

        public abstract IReadOnlyList<IQuestTemplate> GetQuestTemplates();
        
        public virtual async Task<IList<IQuestObjective>> GetQuestObjectives(uint questId) => new List<IQuestObjective>();

        public virtual async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => null;
        
        public virtual async Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId) => null;
        
        public abstract Task<List<IQuestTemplate>> GetQuestTemplatesAsync();

        public abstract IQuestTemplate? GetQuestTemplate(uint entry);

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
            using var model = Database();
            return model.GameObjectTemplate.FirstOrDefault(g => g.Entry == entry);
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
                    (c.SourceType, keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceGroup) ? (int?) c.SourceGroup : null,
                        keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceEntry) ? (int?) c.SourceEntry : null,
                        keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceId) ? (int?) c.SourceId : null))
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
                                                     (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceGroup) || x.SourceGroup == (key.SourceGroup ?? 0)) &&
                                                     (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceEntry)  || x.SourceEntry == (key.SourceEntry ?? 0)) &&
                                                     (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceId)  || x.SourceId == (key.SourceId ?? 0)))
                .ToListAsync<IConditionLine>();
        }

        public async Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys)
        {
            if (manualKeys.Count == 0)
                return new List<IConditionLine>();
            
            await using var model = Database();
            var predicate = PredicateBuilder.New<MySqlConditionLine>();
            foreach (var key in manualKeys)
            {
                predicate = predicate.Or(x => x.SourceType == key.SourceType &&
                                              (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceGroup) || x.SourceGroup == (key.SourceGroup ?? 0)) &&
                                              (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceEntry)  || x.SourceEntry == (key.SourceEntry ?? 0)) &&
                                              (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceId)  || x.SourceId == (key.SourceId ?? 0)));
            }
            return await model.Conditions.Where(predicate).ToListAsync<IConditionLine>();
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

        public virtual async Task<IList<IPlayerChoice>?> GetPlayerChoicesAsync() => null;

        public virtual async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => null;

        public virtual async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => null;

        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId)
        {
            using var model = Database();
            return model.SpellScriptNames.Where(spell => spell.SpellId == spellId).ToList();
        }

        public abstract Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text);

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
            await using var model = Database();
            var predicate = PredicateBuilder.New<MySqlSmartScriptLine>();
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

        public virtual async Task<IPhaseName?> GetPhaseNameAsync(uint phaseId) => null;

        public virtual async Task<IList<IPhaseName>?> GetPhaseNamesAsync() => null;

        public virtual IList<IPhaseName>? GetPhaseNames() => null;
        
        public abstract Task<IList<ICreatureAddon>> GetCreatureAddons();

        public abstract Task<IList<ICreatureTemplateAddon>> GetCreatureTemplateAddons();

        public abstract Task<ICreatureAddon?> GetCreatureAddon(uint guid);

        public abstract Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry);
        
        public virtual async Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId)
        {
            await using var model = Database();
            return await model.WaypointData.Where(wp => wp.PathId == pathId).OrderBy(wp => wp.PointId).ToListAsync<IWaypointData>();
        }

        public virtual async Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId)
        {
            await using var model = Database();
            return await model.SmartScriptWaypoint.Where(wp => wp.PathId == pathId).OrderBy(wp => wp.PointId).ToListAsync<ISmartScriptWaypoint>();
        }

        public async Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId)
        {
            await using var model = Database();
            return await model.ScriptWaypoint.Where(wp => wp.PathId == pathId).OrderBy(wp => wp.PointId).ToListAsync<IScriptWaypoint>();
        }

        public async Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId) => null;
        
        public async Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid) => null;

        public async Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId) => null;

        public async Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId) => null;

        public async Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates()
        {
            await using var model = Database();
            return await model.EquipmentTemplate.ToListAsync<ICreatureEquipmentTemplate>();
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
            await using var model = Database();
            return await model.EquipmentTemplate.Where(x => x.Entry == entry).ToListAsync<ICreatureEquipmentTemplate>();
        }

        public async Task<IList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates() => null;

        public abstract Task<IGameObject?> GetGameObjectByGuidAsync(uint guid);

        public abstract Task<ICreature?> GetCreaturesByGuidAsync(uint guid);

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
    
        public async Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type)
        {
            await using var model = Database();
            return await model.SpawnGroupSpawns.FirstOrDefaultAsync<ISpawnGroupSpawn>(x => x.Guid == guid && x.Type == type);
        }
        
        public async Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id) => null;
        public async Task<IList<ISpawnGroupFormation>?> GetSpawnGroupFormations() => null;
        
        private bool Supports<R>()
        {
            return !currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(R));
        }
    }
}
