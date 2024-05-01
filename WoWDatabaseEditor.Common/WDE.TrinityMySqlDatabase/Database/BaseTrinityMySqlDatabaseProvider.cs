using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqKit;
using LinqToDB;
using LinqToDB.Data;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.TrinityMySqlDatabase.Models;

namespace WDE.TrinityMySqlDatabase.Database
{
    public abstract class BaseTrinityMySqlDatabaseProvider<T> : IAsyncDatabaseProvider, IAuthDatabaseProvider where T : BaseTrinityDatabase, new()
    {
        public bool IsConnected => true;
        public abstract Task<ICreatureTemplate?> GetCreatureTemplate(uint entry);
        public abstract IReadOnlyList<ICreatureTemplate> GetCreatureTemplates();
        public abstract Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry);

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
        
        public async Task<IReadOnlyList<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry)
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
        
        public async Task<IReadOnlyList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList)
        {
            await using var model = Database();
            return await model.BaseSmartScript.Where(line =>
                    (line.ActionType == 80 && line.ActionParam1 == timedActionList) ||
                    (line.ActionType == 88 && timedActionList >= line.ActionParam1 && timedActionList <= line.ActionParam2) ||
                   (line.ActionType == 87 && (line.ActionParam1 == timedActionList || line.ActionParam2 == timedActionList || line.ActionParam3 == timedActionList || line.ActionParam4 == timedActionList || line.ActionParam5 == timedActionList || line.ActionParam6 == timedActionList)))
                .ToListAsync<ISmartScriptLine>();
        }

        public async Task<IReadOnlyList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType)
        {
            var type = (int)scriptType;
            await using var model = Database();
            return await model.BaseSmartScript
                .Where(t => t.ScriptSourceType == type)
                .GroupBy(t => t.EntryOrGuid, t => t.EntryOrGuid)
                .Select(t => t.Key)
                .ToListAsync();
        }

        public abstract IEnumerable<ISmartScriptLine> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type);

        public abstract Task<IReadOnlyList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type);

        public abstract Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions);
        
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
        
        public async Task<IReadOnlyList<IGameEvent>> GetGameEventsAsync()
        {
            await using var model = Database();
            return await (from t in model.GameEvents orderby t.Entry select t).ToListAsync<IGameEvent>();
        }
        
        public IEnumerable<IConversationTemplate> GetConversationTemplates()
        {
            if (!Supports<IConversationTemplate>())
                return new List<IConversationTemplate>();
            
            using var model = Database();
            return (from t in model.ConversationTemplate orderby t.Id select t).ToList<IConversationTemplate>();
        }

        public abstract void ConnectOrThrow();
        public abstract Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync();
        public abstract Task<IReadOnlyList<ICreature>> GetCreaturesAsync();

        public async Task<IReadOnlyList<IConversationTemplate>> GetConversationTemplatesAsync()
        {
            if (!Supports<IConversationTemplate>())
                return new List<IConversationTemplate>();
            
            await using var model = Database();
            return await (from t in model.ConversationTemplate orderby t.Id select t).ToListAsync<IConversationTemplate>();
        }
        
        public IEnumerable<IGossipMenu> GetGossipMenus()
        {
            using var model = Database();

            List<MySqlGossipMenuLine> gossips;
            if (!Supports<INpcText>())
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
        
        public async Task<IReadOnlyList<IGossipMenu>> GetGossipMenusAsync()
        {
            await using var model = Database();

            List<MySqlGossipMenuLine> gossips;
            if (!Supports<INpcText>())
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
            if (!Supports<INpcText>())
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

        public abstract Task<IReadOnlyList<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId);
        public abstract List<IGossipMenuOption> GetGossipMenuOptions(uint menuId);

        public async Task<INpcText?> GetNpcText(uint entry)
        {
            if (!Supports<INpcText>())
                return null;

            await using var model = Database();
            return await model.NpcTexts.FirstOrDefaultAsync(text => text.Id == entry);
        }

        public IEnumerable<INpcText> GetNpcTexts()
        {
            if (!Supports<INpcText>())
                return new List<INpcText>();
            
            using var model = Database();
            return (from t in model.NpcTexts orderby t.Id select t).ToList<INpcText>();
        }

        public async Task<IReadOnlyList<INpcText>> GetNpcTextsAsync()
        {
            if (!Supports<INpcText>())
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
            if (!Supports<IAreaTriggerTemplate>())
                return new List<IAreaTriggerTemplate>();

            using var model = Database();
            return (from t in model.AreaTriggerTemplate orderby t.Id select t).ToList<IAreaTriggerTemplate>();
        }
        
        public async Task<IReadOnlyList<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync()
        {
            if (!Supports<ICreatureClassLevelStat>())
                return new List<ICreatureClassLevelStat>();

            await using var model = Database();
            return await (from t in model.CreatureClassLevelStats select t).ToListAsync<ICreatureClassLevelStat>();
        }

        public abstract Task<IReadOnlyList<IBroadcastText>> GetBroadcastTextsAsync();
        
        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats()
        {
            if (!Supports<ICreatureClassLevelStat>())
                return Enumerable.Empty<ICreatureClassLevelStat>();

            using var model = Database();
            return (from t in model.CreatureClassLevelStats select t).ToList<ICreatureClassLevelStat>();
        }

        public async Task<IReadOnlyList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync()
        {
            if (!Supports<IAreaTriggerTemplate>())
                return new List<IAreaTriggerTemplate>();

            await using var model = Database();
            return await (from t in model.AreaTriggerTemplate orderby t.Id select t).ToListAsync<IAreaTriggerTemplate>();
        }
        
        public IReadOnlyList<IGameObjectTemplate> GetGameObjectTemplates()
        {
            using var model = Database();
            return (from t in model.GameObjectTemplate orderby t.Entry select t).ToList<IGameObjectTemplate>();
        }
        
        public async Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectTemplatesAsync()
        {
            await using var model = Database();
            return await (from t in model.GameObjectTemplate orderby t.Entry select t).ToListAsync<IGameObjectTemplate>();
        }

        public abstract Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync();
        public abstract Task<IReadOnlyList<ICreature>> GetCreaturesAsync(IEnumerable<SpawnKey> guids);
        public abstract Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync(IEnumerable<SpawnKey> guids);

        public abstract IReadOnlyList<IQuestTemplate> GetQuestTemplates();
        
        public virtual async Task<IReadOnlyList<IQuestObjective>> GetQuestObjectives(uint questId) => new List<IQuestObjective>();

        public virtual async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => null;
        
        public virtual async Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId) => null;
        
        public abstract Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesAsync();

        public abstract Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesBySortIdAsync(int questSortId);

        public abstract Task<IQuestTemplate?> GetQuestTemplate(uint entry);

        public async Task<IGameObjectTemplate?> GetGameObjectTemplate(uint entry)
        {
            using var model = Database();
            return model.GameObjectTemplate.FirstOrDefault(g => g.Entry == entry);
        }

        protected abstract Task<ICreature?> GetCreatureByGuidAsync(T model, uint guid);
        protected abstract Task<IGameObject?> GetGameObjectByGuidAsync(T model, uint guid);
        protected abstract Task SetCreatureTemplateAI(T model, uint entry, string ainame, string scriptname);

        public virtual async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(int sourceType, int sourceEntry, int sourceId)
        {
            await using var model = Database();

            return await model.Conditions.Where(line =>
                    line.SourceType == sourceType && line.SourceEntry == sourceEntry && line.SourceId == sourceId)
                .ToListAsync<IConditionLine>();
        }

        public virtual async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key)
        {
            await using var model = Database();

            return await model.Conditions.Where(x => x.SourceType == key.SourceType &&
                                                     (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceGroup) || x.SourceGroup == (key.SourceGroup ?? 0)) &&
                                                     (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceEntry)  || x.SourceEntry == (key.SourceEntry ?? 0)) &&
                                                     (!keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceId)  || x.SourceId == (key.SourceId ?? 0)))
                .ToListAsync<IConditionLine>();
        }

        public virtual async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys)
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
        
        public virtual async Task<IReadOnlyList<IPlayerChoice>?> GetPlayerChoicesAsync() => null;

        public virtual async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => null;

        public virtual async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => null;

        public async Task<IReadOnlyList<ISpellScriptName>> GetSpellScriptNames(int spellId)
        {
            await using var model = Database();
            return await model.SpellScriptNames.Where(spell => spell.SpellId == spellId).ToListAsync();
        }

        public abstract Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text);

        public abstract Task<ICreature?> GetCreatureByGuidAsync(uint entry, uint guid);

        public abstract Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid);

        public abstract IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry);
        public abstract Task<IReadOnlyList<ICreature>> GetCreaturesByEntryAsync(uint entry);
        public abstract Task<IReadOnlyList<IGameObject>> GetGameObjectsByEntryAsync(uint entry);

        public abstract IEnumerable<ICreature> GetCreaturesByEntry(uint entry);


        public abstract IReadOnlyList<ICreature> GetCreatures();
        public abstract Task<IReadOnlyList<ICreature>> GetCreaturesByMapAsync(int map);
        public abstract Task<IReadOnlyList<IGameObject>> GetGameObjectsByMapAsync(int map);
        public abstract IEnumerable<IGameObject> GetGameObjects();

        public async Task<IReadOnlyList<ICoreCommandHelp>> GetCommands()
        {
            using var model = Database();
            return model.Commands.ToList();
        }

        public abstract Task<IReadOnlyList<ITrinityString>> GetStringsAsync();
        public abstract Task<IReadOnlyList<IDatabaseSpellDbc>> GetSpellDbcAsync();

        public async Task<IReadOnlyList<IPointOfInterest>> GetPointsOfInterestsAsync()
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

        public virtual Task<IReadOnlyList<IItem>?> GetItemTemplatesAsync() => Task.FromResult<IReadOnlyList<IItem>?>(null);

        protected ExpressionStarter<TR> GenerateWhereConditionsForEventScript<TR>(IEnumerable<(uint command, int dataIndex, long valueToSearch)> conditions) where TR : IEventScriptLine
        {
            var predicate = PredicateBuilder.New<TR>();
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

        public abstract Task<IReadOnlyList<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions);

        public abstract Task<IReadOnlyList<ICreatureModelInfo>> GetCreatureModelInfoAsync();

        public abstract Task<ICreatureModelInfo?> GetCreatureModelInfo(uint displayId);

        public abstract Task<IReadOnlyList<IEventScriptLine>> GetEventScript(EventScriptType type, uint id);

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

        public async Task<IReadOnlyList<ISceneTemplate>?> GetSceneTemplatesAsync()
        {
            if (!Supports<ISceneTemplate>())
                return null; 
            await using var model = Database();
            return await model.SceneTemplates.ToListAsync<ISceneTemplate>();
        }

        public virtual async Task<IPhaseName?> GetPhaseNameAsync(uint phaseId) => null;

        public virtual async Task<IReadOnlyList<IPhaseName>?> GetPhaseNamesAsync() => null;
        
        public async Task<IReadOnlyList<INpcSpellClickSpell>> GetNpcSpellClickSpells(uint creatureId)
        {
            return Array.Empty<INpcSpellClickSpell>();
        }

        public virtual IList<IPhaseName>? GetPhaseNames() => null;
        
        public abstract Task<IReadOnlyList<ICreatureAddon>> GetCreatureAddons();

        public abstract Task<IReadOnlyList<ICreatureTemplateAddon>> GetCreatureTemplateAddons();

        public abstract Task<ICreatureAddon?> GetCreatureAddon(uint entry, uint guid);

        public abstract Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry);

        public abstract Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId);

        public virtual async Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId, uint count)
        {
            await using var model = Database();
            return await model.SmartScriptWaypoint.Where(wp => wp.PathId >= pathId && wp.PathId < pathId + count).OrderBy(wp => wp.PointId).ToListAsync<ISmartScriptWaypoint>();
        }

        public virtual async Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId)
        {
            await using var model = Database();
            return await model.ScriptWaypoint.Where(wp => wp.PathId == pathId).OrderBy(wp => wp.PointId).ToListAsync<IScriptWaypoint>();
        }

        public async Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId) => null;
        
        public async Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid) => null;

        public async Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId) => null;

        public async Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId) => null;

        public async Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates()
        {
            await using var model = Database();
            return await model.EquipmentTemplate.ToListAsync<ICreatureEquipmentTemplate>();
        }

        public async Task<IReadOnlyList<IGameEventCreature>> GetGameEventCreaturesAsync()
        {
            await using var model = Database();
            return await model.GameEventCreature.ToListAsync<IGameEventCreature>();
        }

        public async Task<IReadOnlyList<IGameEventGameObject>> GetGameEventGameObjectsAsync()
        {
            await using var model = Database();
            return await model.GameEventGameObject.ToListAsync<IGameEventGameObject>();
        }

        public async Task<IReadOnlyList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint entry, uint guid)
        {
            await using var model = Database();
            return await model.GameEventCreature.Where(x => x.Guid == guid).ToListAsync<IGameEventCreature>();
        }

        public async Task<IReadOnlyList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint entry, uint guid)
        {
            await using var model = Database();
            return await model.GameEventGameObject.Where(x => x.Guid == guid).ToListAsync<IGameEventGameObject>();
        }

        public async Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry)
        {
            await using var model = Database();
            return await model.EquipmentTemplate.Where(x => x.Entry == entry).ToListAsync<ICreatureEquipmentTemplate>();
        }

        public async Task<IReadOnlyList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates() => null;

        public abstract Task<ICreature?> GetCreaturesByGuidAsync(uint entry, uint guid);

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

        
        public async Task<IReadOnlyList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync()
        {
            if (!Supports<ISpawnGroupTemplate>())
                return new List<ISpawnGroupTemplate>();
            await using var model = Database();
            return await model.SpawnGroupTemplate.ToListAsync<ISpawnGroupTemplate>();
        }
    
        public async Task<IReadOnlyList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync()
        {
            if (!Supports<ISpawnGroupSpawn>())
                return new List<ISpawnGroupSpawn>();
            await using var model = Database();
            return await model.SpawnGroupSpawns.ToListAsync<ISpawnGroupSpawn>();
        }
    
        public async Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id)
        {
            if (!Supports<ISpawnGroupTemplate>())
                return null;
            await using var model = Database();
            return await model.SpawnGroupTemplate.FirstOrDefaultAsync<ISpawnGroupTemplate>(x => x.Id == id);
        }
    
        public async Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type)
        {
            if (!Supports<ISpawnGroupSpawn>())
                return null;
            await using var model = Database();
            return await model.SpawnGroupSpawns.FirstOrDefaultAsync<ISpawnGroupSpawn>(x => x.Guid == guid && x.Type == type);
        }
        
        public async Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id) => null;
        public async Task<IReadOnlyList<ISpawnGroupFormation>?> GetSpawnGroupFormations() => null;
        
        public virtual async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type)
        {
            await using var database = Database();
            switch (type)
            {
                case LootSourceType.Item:
                    return await database.ItemLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.GameObject:
                    return await database.GameObjectLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Fishing:
                    return await database.FishingLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Pickpocketing:
                    return await database.PickpocketingLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Skinning:
                    return await database.SkinningLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Disenchant:
                    return await database.DisenchantLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Prospecting:
                    return await database.ProspectingLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Milling:
                    return await database.MillingLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Reference:
                    return await database.ReferenceLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Creature:
                    return await database.CreatureLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Mail:
                    return await database.MailLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Spell:
                    return await database.SpellLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.Item)
                        .ToListAsync<ILootEntry>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public async Task<ILootTemplateName?> GetLootTemplateName(LootSourceType type, uint entry) => null;
        
        public async Task<IReadOnlyList<ILootTemplateName>> GetLootTemplateName(LootSourceType type) => Array.Empty<ILootTemplateName>();

        public abstract Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)>
            GetCreatureLootCrossReference(uint lootId);

        public abstract Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)>
            GetCreatureSkinningLootCrossReference(uint lootId);

        public abstract Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)>
            GetCreaturePickPocketLootCrossReference(uint lootId);

        public async Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectLootCrossReference(uint lootId)
        {
            await using var database = Database();
            return await database.GameObjectTemplate.Where(template =>
                template.Type == GameobjectType.Chest && template.Data1 == lootId).ToListAsync();
        }

        public virtual async Task<IReadOnlyList<ILootEntry>> GetReferenceLootCrossReference(uint lootId)
        {
            if (lootId == 0)
                return Array.Empty<ILootEntry>();
            await using var database = Database();
            var loot = new[]
            {
                await database.CreatureLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.GameObjectLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.ItemLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.FishingLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.PickpocketingLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.SkinningLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.DisenchantLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.ProspectingLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.MillingLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.MailLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>(),
                await database.SpellLootTemplate.Where(x => x.Reference == lootId).ToListAsync<ILootEntry>()
            };
            return loot.SelectMany(x => x).ToList();
        }

        public virtual async Task<IReadOnlyList<IConversationActor>> GetConversationActors()
        {
            return Array.Empty<IConversationActor>();
        }

        public virtual async Task<IReadOnlyList<IConversationActorTemplate>> GetConversationActorTemplates()
        {
            return Array.Empty<IConversationActorTemplate>();
        }

        public virtual async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type, uint entry)
        {
            await using var database = Database();
            switch (type)
            {
                case LootSourceType.Item:
                    return await database.ItemLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.GameObject:
                    return await database.GameObjectLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Fishing:
                    return await database.FishingLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Pickpocketing:
                    return await database.PickpocketingLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Skinning:
                    return await database.SkinningLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Disenchant:
                    return await database.DisenchantLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Prospecting:
                    return await database.ProspectingLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Milling:
                    return await database.MillingLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Reference:
                    return await database.ReferenceLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Creature:
                    return await database.CreatureLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Mail:
                    return await database.MailLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Spell:
                    return await database.SpellLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public async Task<IReadOnlyList<IQuestRelation>> GetQuestStarters(uint questId)
        {
            await using var model = Database();
            var creatures = await model.CreatureQuestStarters.Where(x => x.Quest == questId).ToListAsync<IQuestRelation>();
            var gameobjects = await model.GameObjectQuestStarters.Where(x => x.Quest == questId).ToListAsync<IQuestRelation>();
            creatures.AddRange(gameobjects);
            return creatures;
        }

        public async Task<IReadOnlyList<IQuestRelation>> GetQuestEnders(uint questId)
        {
            await using var model = Database();
            var creatures = await model.CreatureQuestEnders.Where(x => x.Quest == questId).ToListAsync<IQuestRelation>();
            var gameobjects = await model.GameObjectQuestEnders.Where(x => x.Quest == questId).ToListAsync<IQuestRelation>();
            creatures.AddRange(gameobjects);
            return creatures;
        }

        private bool Supports<R>()
        {
            return !currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(R));
        }
    }
}
