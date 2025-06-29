using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KTrie;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.QueryGenerators.Base;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public class CachedDatabaseProvider : ICachedDatabaseProvider
    {
        private Dictionary<(uint entry, uint guid), ICreature> creaturesByGuid = new();
        private Dictionary<(uint entry, uint guid), IGameObject> gameObjectsByGuid = new();

        private IReadOnlyList<ICreatureTemplate>? creatureTemplateCache;
        private Dictionary<uint, ICreatureTemplate> creatureTemplateByEntry = new();
        
        private IReadOnlyList<IGameObjectTemplate>? gameObjectTemplateCache;
        private Dictionary<uint, IGameObjectTemplate> gameObjectTemplateByEntry = new();

        private IReadOnlyList<IQuestTemplate>? questTemplateCache;
        private Dictionary<uint, IQuestTemplate> questTemplateByEntry = new();

        private Dictionary<uint, ISceneTemplate> sceneByEntry = new();
        
        private Dictionary<uint, ICreatureModelInfo>? creatureModelInfos;
        
        private Dictionary<uint, IReadOnlyList<ICreatureText>?> creatureTextsCache = new();

        private IReadOnlyList<IAreaTriggerTemplate>? areaTriggerTemplates;
        private IReadOnlyList<IGameEvent>? gameEventsCache;
        private IReadOnlyList<IConversationTemplate>? conversationTemplates;
        private IReadOnlyList<IGossipMenu>? gossipMenusCache;
        private IReadOnlyList<INpcText>? npcTextsCache;
        private Dictionary<uint, INpcText>? npcTextsByIdCache;
        private IReadOnlyList<ICreatureClassLevelStat>? creatureClassLevelStatsCache;
        private IReadOnlyList<IDatabaseSpellDbc>? databaseSpellDbcCache;

        private IReadOnlyList<IBroadcastText>? broadcastTextsSortedCache;
        private Dictionary<uint, int>? broadcastTextIndexByIdCache;
        private TrieDictionary<IBroadcastText>? broadcastTextsCache = new();
        
        private IAsyncDatabaseProvider nonCachedDatabase;
        private readonly ITaskRunner taskRunner;
        private readonly IStatusBar statusBar;
        private readonly IEventAggregator eventAggregator;
        private readonly ILoadingEventAggregator loadingEventAggregator;
        private readonly IParameterFactory parameterFactory;

        public CachedDatabaseProvider(IAsyncDatabaseProvider nonCachedDatabase,
            ITaskRunner taskRunner, IStatusBar statusBar, IEventAggregator eventAggregator,
            ILoadingEventAggregator loadingEventAggregator,
            IQueryGenerator<ICreatureText> creatureTextInsertProvider,
            IParameterFactory parameterFactory)
        {
            this.nonCachedDatabase = nonCachedDatabase;
            this.taskRunner = taskRunner;
            this.statusBar = statusBar;
            this.eventAggregator = eventAggregator;
            this.loadingEventAggregator = loadingEventAggregator;
            this.parameterFactory = parameterFactory;
            eventAggregator.GetEvent<DatabaseTableChanged>().Subscribe(tableName =>
            {
                if (tableName == DatabaseTable.WorldTable("creature_template"))
                    Refresh(RefreshCreatureTemplate);
                else if (tableName == DatabaseTable.WorldTable("gameobject_template"))
                    Refresh(RefreshGameObjectTemplate);
                //if (tableName == "creature")
                //    Refresh(RefreshCreature);
                else if (tableName == DatabaseTable.WorldTable("gossip_menu"))
                    Refresh(RefreshGossipMenu);
                else if (tableName == DatabaseTable.WorldTable("npc_text"))
                    Refresh(RefreshNpcTexts);
                else if (tableName == creatureTextInsertProvider.TableName)
                    Refresh(RefreshCreatureTexts);
                else if (tableName == DatabaseTable.WorldTable("phase_name") || tableName == DatabaseTable.WorldTable("phase_names"))
                    Refresh(() => Task.FromResult(typeof(IPhaseName)));
                else if (tableName == DatabaseTable.WorldTable("quest_template") || tableName == DatabaseTable.WorldTable("quest_template_addon"))
                    Refresh(RefreshQuestTemplates);
                else if (tableName == DatabaseTable.WorldTable("areatrigger_template"))
                    Refresh(RefreshAreatriggerTemplates);
            }, true);
        }

        private void Refresh(Func<Task<Type>> refreshingFunc)
        {
            var taskCompletionSource = new TaskCompletionSource();
            refreshTasks.Add(taskCompletionSource.Task);
            taskRunner.ScheduleTask("Refresh database", async () =>
                {
                    try
                    {
                        var type = await refreshingFunc();
                        eventAggregator.GetEvent<DatabaseCacheReloaded>().Publish(type);
                    }
                    finally
                    {
                        refreshTasks.Remove(taskCompletionSource.Task);
                        taskCompletionSource.SetResult();
                    }
                }).ListenErrors();
        }
        
        private async Task<Type> RefreshNpcTexts()
        {
            npcTextsCache = await nonCachedDatabase.GetNpcTextsAsync().ConfigureAwait(false);
            npcTextsByIdCache = npcTextsCache.ToDictionary(npcText => npcText.Id);
            return typeof(INpcText);
        }
        
        private async Task<Type> RefreshCreatureTexts()
        {
            creatureTextsCache.Clear();
            return typeof(ICreatureText);
        }
        

        private async Task<Type> RefreshGossipMenu()
        {
            gossipMenusCache = await nonCachedDatabase.GetGossipMenusAsync().ConfigureAwait(false);
            return typeof(IGossipMenu);
        }

        private async Task<Type> RefreshCreatureTemplate()
        {
            creatureTemplateCache = null;
            creatureTemplateByEntry.Clear();
            var templates = await nonCachedDatabase.GetCreatureTemplatesAsync().ConfigureAwait(false);
            Dictionary<uint, ICreatureTemplate> tempDict = templates.ToDictionary(t => t.Entry);
            creatureTemplateCache = templates;
            creatureTemplateByEntry = tempDict;
            return typeof(ICreatureTemplate);
        }

        private async Task<Type> RefreshGameObjectTemplate()
        {
            gameObjectTemplateCache = null;
            gameObjectTemplateByEntry.Clear();
            var templates = await nonCachedDatabase.GetGameObjectTemplatesAsync().ConfigureAwait(false);
            Dictionary<uint, IGameObjectTemplate> tempDict = templates.ToDictionary(t => t.Entry);
            gameObjectTemplateCache = templates;
            gameObjectTemplateByEntry = tempDict;
            return typeof(ICreatureTemplate);
        }
        
        private async Task<Type> RefreshQuestTemplates()
        {
            var templates = await nonCachedDatabase.GetQuestTemplatesAsync().ConfigureAwait(false);
            Dictionary<uint, IQuestTemplate> tempDict = templates.ToDictionary(t => t.Entry);
            questTemplateCache = templates;
            questTemplateByEntry = tempDict;
            return typeof(IQuestTemplate);
        }

        private async Task<Type> RefreshAreatriggerTemplates()
        {
            var templates = await nonCachedDatabase.GetAreaTriggerTemplatesAsync().ConfigureAwait(false);
            areaTriggerTemplates = templates;
            return typeof(IAreaTriggerTemplate);
        }
        
        public Task TryConnect()
        {
            nonCachedDatabase.ConnectOrThrow(); // if there is some connection problem, it should throw
            cacheInProgress = true;
            var task = taskRunner.ScheduleTask(new DatabaseCacheTask(this));
            cacheTask = task;
            return task;
        }

        public bool IsConnected => nonCachedDatabase.IsConnected;
        public async Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync()
        {
            if (creatureTemplateCache != null)
                return creatureTemplateCache;
            return await nonCachedDatabase.GetCreatureTemplatesAsync();
        }

        public async Task<IReadOnlyList<IConversationTemplate>> GetConversationTemplatesAsync()
        {
            if (conversationTemplates != null)
                return conversationTemplates;
            return await nonCachedDatabase.GetConversationTemplatesAsync();
        }

        public async Task<IReadOnlyList<IGameEvent>> GetGameEventsAsync()
        {
            if (gameEventsCache != null)
                return gameEventsCache;
            return await nonCachedDatabase.GetGameEventsAsync();
        }

        public async Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectTemplatesAsync()
        {
            if (gameObjectTemplateCache != null)
                return gameObjectTemplateCache;
            return await nonCachedDatabase.GetGameObjectTemplatesAsync();
        }

        public async Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesAsync()
        {
            if (questTemplateCache != null)
                return questTemplateCache;
            return await nonCachedDatabase.GetQuestTemplatesAsync();
        }

        public async Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesBySortIdAsync(int zoneSortId)
        {
            if (questTemplateCache != null)
                return questTemplateCache.Where(q => q.QuestSortId == zoneSortId).ToList();
            return await WaitForCache(nonCachedDatabase.GetQuestTemplatesBySortIdAsync(zoneSortId));
        }

        public async Task<IReadOnlyList<INpcText>> GetNpcTextsAsync()
        {
            if (npcTextsCache != null)
                return npcTextsCache;
            return await nonCachedDatabase.GetNpcTextsAsync();
        }

        public async Task<IReadOnlyList<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync()
        {
            if (creatureClassLevelStatsCache != null)
                return creatureClassLevelStatsCache;
            return await nonCachedDatabase.GetCreatureClassLevelStatsAsync();
        }

        public async Task<ICreatureTemplate?> GetCreatureTemplate(uint entry)
        {
            await WaitForCache();
            
            if (entry == 0)
                return null;

            if (creatureTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            template = await nonCachedDatabase.GetCreatureTemplate(entry);
            if (template != null)
                creatureTemplateByEntry[entry] = template;
            return template;
        }

        public ICreatureTemplate? GetCachedCreatureTemplate(uint entry)
        {
            if (entry == 0)
                return null;

            if (creatureTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return null;
        }

        public ICreature? GetCachedCreatureByGuid(uint entry, uint guid)
        {
            if (creaturesByGuid.TryGetValue((entry, guid), out var creature))
                return creature;
            return null;
        }

        public IGameObject? GetCachedGameObjectByGuid(uint entry, uint guid)
        {
            if (gameObjectsByGuid.TryGetValue((entry, guid), out var gameObject))
                return gameObject;
            return null;
        }

        public IReadOnlyList<ICreatureTemplate>? GetCachedCreatureTemplates()
        {
            return creatureTemplateCache;
        }

        public IReadOnlyList<IGameObjectTemplate>? GetCachedGameobjectTemplates()
        {
            return gameObjectTemplateCache;
        }

        public Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry)
        {
            return nonCachedDatabase.GetCreatureTemplateDifficulties(entry);
        }

        public async Task<IGameObjectTemplate?> GetGameObjectTemplate(uint entry)
        {
            await WaitForCache();

            if (entry == 0)
                return null;
            
            if (gameObjectTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            template = await nonCachedDatabase.GetGameObjectTemplate(entry);
            if (template != null)
                gameObjectTemplateByEntry[entry] = template;
            return template;
        }

        public IGameObjectTemplate? GetCachedGameObjectTemplate(uint entry)
        {
            if (entry == 0)
                return null;
            
            if (gameObjectTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return null;
        }

        public IReadOnlyList<IGameObjectTemplate>? GetCachedGameObjectTemplates()
        {
            return gameObjectTemplateCache;
        }

        public async Task<IQuestTemplate?> GetQuestTemplate(uint entry)
        {
            await WaitForCache();

            if (questTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return await nonCachedDatabase.GetQuestTemplate(entry);
        }

        public IQuestTemplate? GetCachedQuestTemplate(uint entry)
        {
            if (questTemplateByEntry.TryGetValue(entry, out var template))
                return template;
            return null;
        }

        public ISceneTemplate? GetCachedSceneTemplate(uint entry)
        {
            if (sceneByEntry.TryGetValue(entry, out var scene))
                return scene;
            return null;
        }

        public Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry) => WaitForCache(nonCachedDatabase.GetAreaTriggerScript(entry));
        
        public Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry) => WaitForCache(nonCachedDatabase.GetAreaTriggerTemplate(entry));

        public async Task<IReadOnlyList<IGameObject>> GetGameObjectsByEntryAsync(uint entry)
        {
            await WaitForCache();
            return await nonCachedDatabase.GetGameObjectsByEntryAsync(entry);
        }

        public Task<IReadOnlyList<ICreature>> GetCreaturesAsync() => WaitForCache(nonCachedDatabase.GetCreaturesAsync());

        public Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync() => WaitForCache(nonCachedDatabase.GetGameObjectsAsync());

        public Task<IReadOnlyList<ICreature>> GetCreaturesAsync(IEnumerable<SpawnKey> guids) => nonCachedDatabase.GetCreaturesAsync(guids);

        public Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync(IEnumerable<SpawnKey> guids) => nonCachedDatabase.GetGameObjectsAsync(guids);

        public Task<IReadOnlyList<ICreature>> GetCreaturesByMapAsync(int map) => WaitForCache(nonCachedDatabase.GetCreaturesByMapAsync(map));
        
        public Task<IReadOnlyList<IGameObject>> GetGameObjectsByMapAsync(int map) => WaitForCache(nonCachedDatabase.GetGameObjectsByMapAsync(map));

        public Task<IReadOnlyList<IQuestObjective>> GetQuestObjectives(uint questId) => WaitForCache(nonCachedDatabase.GetQuestObjectives(questId));

        public Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => WaitForCache(nonCachedDatabase.GetQuestObjective(questId, storageIndex));

        public Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId) => WaitForCache(nonCachedDatabase.GetQuestObjectiveById(objectiveId));

        public Task<IQuestRequestItem?> GetQuestRequestItem(uint entry) => WaitForCache(nonCachedDatabase.GetQuestRequestItem(entry));

        public async Task<IReadOnlyList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync()
        {
            await WaitForCache();
            return areaTriggerTemplates ?? await nonCachedDatabase.GetAreaTriggerTemplatesAsync();
        }

        public Task<IReadOnlyList<IGossipMenu>> GetGossipMenusAsync() => gossipMenusCache == null
            ? WaitForCache(nonCachedDatabase.GetGossipMenusAsync())
            : Task.FromResult(gossipMenusCache);

        public Task<IGossipMenu?> GetGossipMenuAsync(uint menuId) => WaitForCache(nonCachedDatabase.GetGossipMenuAsync(menuId));

        public Task<IReadOnlyList<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId) => WaitForCache(nonCachedDatabase.GetGossipMenuOptionsAsync(menuId));

        public async Task<INpcText?> GetNpcText(uint entry)
        {
            await WaitForCache();
            if (npcTextsByIdCache != null &&
                npcTextsByIdCache.TryGetValue(entry, out var text))
                return text;
            return await nonCachedDatabase.GetNpcText(entry);
        }

        public Task<IReadOnlyList<IPointOfInterest>> GetPointsOfInterestsAsync() => WaitForCache(nonCachedDatabase.GetPointsOfInterestsAsync());
        
        public async Task<IReadOnlyList<IBroadcastText>> GetBroadcastTextsAsync()
        {
            await WaitForCache();
            return broadcastTextsSortedCache ?? await nonCachedDatabase.GetBroadcastTextsAsync();
        }

        public Task<IReadOnlyList<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry) => WaitForCache(nonCachedDatabase.GetCreatureTextsByEntryAsync(entry));

        public Task<IReadOnlyList<IItem>?> GetItemTemplatesAsync() => WaitForCache(nonCachedDatabase.GetItemTemplatesAsync());
        public Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions) 
            => WaitForCache(nonCachedDatabase.FindSmartScriptLinesBy(conditions));

        public Task<IReadOnlyList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync() => WaitForCache(nonCachedDatabase.GetSpawnGroupTemplatesAsync());
        public Task<IReadOnlyList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync() => WaitForCache(nonCachedDatabase.GetSpawnGroupSpawnsAsync());
        public Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id) => WaitForCache(nonCachedDatabase.GetSpawnGroupTemplateByIdAsync(id));
        public Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type) => WaitForCache(nonCachedDatabase.GetSpawnGroupSpawnByGuidAsync(guid, type));
        public Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id) => WaitForCache(nonCachedDatabase.GetSpawnGroupFormation(id));
        public Task<IReadOnlyList<ISpawnGroupFormation>?> GetSpawnGroupFormations() => WaitForCache(nonCachedDatabase.GetSpawnGroupFormations());

        public Task<IReadOnlyList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList) => WaitForCache(nonCachedDatabase.GetLinesCallingSmartTimedActionList(timedActionList));

        public Task<IReadOnlyList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type) 
            => WaitForCache(nonCachedDatabase.GetScriptForAsync(entry, entryOrGuid, type));

        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(int sourceType, int sourceEntry, int sourceId)
        {
            await WaitForCache();
            return await nonCachedDatabase.GetConditionsForAsync(sourceType, sourceEntry, sourceId);
        }
        
        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key)
        {
            await WaitForCache();
            return await nonCachedDatabase.GetConditionsForAsync(keyMask, key);
        }

        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys)
        {
            await WaitForCache();
            return await nonCachedDatabase.GetConditionsForAsync(keyMask, manualKeys);
        }

        public async Task<IReadOnlyList<ISpellScriptName>> GetSpellScriptNames(int spellId)
        {
            return await nonCachedDatabase.GetSpellScriptNames(spellId);
        }

        public Task<IReadOnlyList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            WaitForCache(nonCachedDatabase.GetSmartScriptEntriesByType(scriptType));

        public Task<IReadOnlyList<IPlayerChoice>?> GetPlayerChoicesAsync() => WaitForCache(nonCachedDatabase.GetPlayerChoicesAsync());

        public Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => WaitForCache(nonCachedDatabase.GetPlayerChoiceResponsesAsync());

        public Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => WaitForCache(nonCachedDatabase.GetPlayerChoiceResponsesAsync(choiceId));

        public async Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            await WaitForCache();
            if (broadcastTextsCache == null)
                return await nonCachedDatabase.GetBroadcastTextByTextAsync(text);
            if (broadcastTextsCache.TryGetValue(text, out var val))
                return val;
            return null;
        }

        public async Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id)
        {
            await WaitForCache();
            if (broadcastTextIndexByIdCache == null)
                return await nonCachedDatabase.GetBroadcastTextByIdAsync(id);
            if (broadcastTextIndexByIdCache.TryGetValue(id, out var index))
                return broadcastTextsSortedCache![index];
            return null;
        }

        public Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text) => WaitForCache(nonCachedDatabase.GetBroadcastTextLocaleByTextAsync(text));

        public Task<IReadOnlyList<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions) 
            => WaitForCache(nonCachedDatabase.FindEventScriptLinesBy(conditions));
        public Task<IReadOnlyList<ICreatureModelInfo>> GetCreatureModelInfoAsync() 
            => WaitForCache(nonCachedDatabase.GetCreatureModelInfoAsync());

        public async Task<ICreatureModelInfo?> GetCreatureModelInfo(uint displayId)
        {
            if (creatureModelInfos == null)
                return await nonCachedDatabase.GetCreatureModelInfo(displayId);
            if (creatureModelInfos.TryGetValue(displayId, out var info))
                return info;
            return null;
        }

        public Task<IReadOnlyList<IQuestRelation>> GetQuestStarters(uint questId) => WaitForCache(nonCachedDatabase.GetQuestStarters(questId));

        public Task<IReadOnlyList<IQuestRelation>> GetQuestEnders(uint questId) => WaitForCache(nonCachedDatabase.GetQuestEnders(questId));

        public Task<IReadOnlyList<IQuestFactionChange>> GetQuestFactionChanges() => nonCachedDatabase.GetQuestFactionChanges();

        public Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId) => WaitForCache(nonCachedDatabase.GetSceneTemplateAsync(sceneId));

        public Task<IReadOnlyList<ISceneTemplate>?> GetSceneTemplatesAsync() => WaitForCache(nonCachedDatabase.GetSceneTemplatesAsync());

        public Task<IPhaseName?> GetPhaseNameAsync(uint phaseId) => WaitForCache(nonCachedDatabase.GetPhaseNameAsync(phaseId));

        public Task<IReadOnlyList<IPhaseName>?> GetPhaseNamesAsync() => WaitForCache(nonCachedDatabase.GetPhaseNamesAsync());

        public Task<IReadOnlyList<INpcSpellClickSpell>> GetNpcSpellClickSpells(uint creatureId) => nonCachedDatabase.GetNpcSpellClickSpells(creatureId);

        public IList<IPhaseName>? GetPhaseNames() => nonCachedDatabase.GetPhaseNames();

        public Task<IReadOnlyList<ICreatureAddon>> GetCreatureAddons() => WaitForCache(nonCachedDatabase.GetCreatureAddons());

        public Task<IReadOnlyList<ICreatureTemplateAddon>> GetCreatureTemplateAddons() => WaitForCache(nonCachedDatabase.GetCreatureTemplateAddons());

        public Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates() => WaitForCache(nonCachedDatabase.GetCreatureEquipmentTemplates());

        public Task<IReadOnlyList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates() => WaitForCache(nonCachedDatabase.GetMangosCreatureEquipmentTemplates());

        public Task<IReadOnlyList<IGameEventCreature>> GetGameEventCreaturesAsync() => WaitForCache(nonCachedDatabase.GetGameEventCreaturesAsync());

        public Task<IReadOnlyList<IGameEventGameObject>> GetGameEventGameObjectsAsync() => WaitForCache(nonCachedDatabase.GetGameEventGameObjectsAsync());

        public Task<IReadOnlyList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetGameEventCreaturesByGuidAsync(entry, guid));

        public Task<IReadOnlyList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetGameEventGameObjectsByGuidAsync(entry, guid));

        public Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry) => WaitForCache(nonCachedDatabase.GetCreatureEquipmentTemplates(entry));

        public Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetGameObjectByGuidAsync(entry, guid));

        public Task<ICreature?> GetCreaturesByGuidAsync(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetCreaturesByGuidAsync(entry, guid));

        public Task<ICreatureAddon?> GetCreatureAddon(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetCreatureAddon(entry, guid));

        public Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry) => WaitForCache(nonCachedDatabase.GetCreatureTemplateAddon(entry));
       
        public Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId) => WaitForCache(nonCachedDatabase.GetWaypointData(pathId));

        public Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId, uint count) => WaitForCache(nonCachedDatabase.GetSmartScriptWaypoints(pathId, count));

        public Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId) => WaitForCache(nonCachedDatabase.GetScriptWaypoints(pathId));

        public Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId) => WaitForCache(nonCachedDatabase.GetMangosWaypoints(pathId));

        public Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid) => WaitForCache(nonCachedDatabase.GetMangosCreatureMovement(guid));
        
        public Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId) => WaitForCache(nonCachedDatabase.GetMangosCreatureMovementTemplate(entry, pathId));
        
        public Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId) => WaitForCache(nonCachedDatabase.GetMangosPathName(pathId));

        public Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type, uint entry) => nonCachedDatabase.GetLoot(type, entry);

        public Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type) => nonCachedDatabase.GetLoot(type);

        public Task<ILootTemplateName?> GetLootTemplateName(LootSourceType type, uint entry) => nonCachedDatabase.GetLootTemplateName(type, entry);
        
        public Task<IReadOnlyList<ILootTemplateName>> GetLootTemplateName(LootSourceType type) => nonCachedDatabase.GetLootTemplateName(type);

        public Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureLootCrossReference(uint lootId) => nonCachedDatabase.GetCreatureLootCrossReference(lootId);

        public Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureSkinningLootCrossReference(uint lootId) => nonCachedDatabase.GetCreatureSkinningLootCrossReference(lootId);

        public Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreaturePickPocketLootCrossReference(uint lootId) => nonCachedDatabase.GetCreaturePickPocketLootCrossReference(lootId);

        public Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectLootCrossReference(uint lootId) => nonCachedDatabase.GetGameObjectLootCrossReference(lootId);

        public Task<IReadOnlyList<ILootEntry>> GetReferenceLootCrossReference(uint lootId) => nonCachedDatabase.GetReferenceLootCrossReference(lootId);

        public Task<IReadOnlyList<IConversationActor>> GetConversationActors() => nonCachedDatabase.GetConversationActors();

        public Task<IReadOnlyList<IConversationActorTemplate>> GetConversationActorTemplates() => nonCachedDatabase.GetConversationActorTemplates();

        public Task<IReadOnlyList<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => WaitForCache(nonCachedDatabase.GetEventScript(type, id));

        public Task<IReadOnlyList<IEventAiLine>> GetEventAi(int id) => WaitForCache(nonCachedDatabase.GetEventAi(id));

        public async Task<IReadOnlyList<IDatabaseSpellDbc>> GetSpellDbcAsync()
        {
            await WaitForCache();
            
            if (databaseSpellDbcCache != null)
                return databaseSpellDbcCache;

            return await nonCachedDatabase.GetSpellDbcAsync();
        }
        
        public Task<IReadOnlyList<IDatabaseSpellEffectDbc>> GetSpellEffectDbcAsync() => WaitForCache(nonCachedDatabase.GetSpellEffectDbcAsync());

        public Task<ICreature?> GetCreatureByGuidAsync(uint entry, uint guid) => nonCachedDatabase.GetCreatureByGuidAsync(entry, guid);

        public Task<IReadOnlyList<ICreature>> GetCreaturesByEntryAsync(uint entry) => WaitForCache(nonCachedDatabase.GetCreaturesByEntryAsync(entry));

        public Task<IReadOnlyList<ICoreCommandHelp>> GetCommands() => nonCachedDatabase.GetCommands();

        public Task<IReadOnlyList<ITrinityString>> GetStringsAsync() => WaitForCache(nonCachedDatabase.GetStringsAsync());

        public Task<IReadOnlyList<IQuestScriptName>> GetQuestScriptNames(uint questId) => WaitForCache(nonCachedDatabase.GetQuestScriptNames(questId));

        private bool cacheInProgress = false;
        private Task cacheTask = Task.CompletedTask;
        private List<Task> refreshTasks = new();

        public async Task WaitForCache()
        {
            if (!cacheInProgress && refreshTasks.Count == 0)
                return;

            if (cacheInProgress)
                await cacheTask;
            // vv this doesn't work with sequential Tasks (waiting for one to finish before starting another)
            //else if (!cacheInProgress)
            //    await Task.WhenAll(refreshTasks.ToList());
            //else
            //{
            //    var tasksToWait = refreshTasks.ToList();
            //    tasksToWait.Add(cacheTask);
            //    await Task.WhenAll(tasksToWait);
            //}
        }

        public async Task<T> WaitForCache<T>(Task<T> inner)
        {
            await WaitForCache();
            return await inner;
        }
        
        private class DatabaseCacheTask : IAsyncTask
        {
            private readonly CachedDatabaseProvider cache;
            public string Name => "Database cache";
            public bool WaitForOtherTasks => false;

            public DatabaseCacheTask(CachedDatabaseProvider cache)
            {
                this.cache = cache;
            }

            public async Task Run(ITaskProgress progress)
            {
                try
                {
                    int steps = 14;

                    progress.Report(0, steps, "Loading creatures");
                    cache.creatureTemplateCache = await cache.nonCachedDatabase.GetCreatureTemplatesAsync();
                    
                    // disabled caching for now, because it may be not needed yet
                    // cache.creatureCache = await cache.nonCachedDatabase.GetCreaturesAsync();
                    // cache.gameObjectCache = await cache.nonCachedDatabase.GetGameObjectsAsync();

                    progress.Report(1, steps, "Loading gameobjects");
                    cache.gameObjectTemplateCache = await cache.nonCachedDatabase.GetGameObjectTemplatesAsync();

                    progress.Report(2, steps, "Loading game events");
                    cache.gameEventsCache = await cache.nonCachedDatabase.GetGameEventsAsync();

                    progress.Report(3, steps, "Loading areatrigger templates");
                    cache.areaTriggerTemplates = await cache.nonCachedDatabase.GetAreaTriggerTemplatesAsync();

                    progress.Report(4, steps, "Loading conversation templates");
                    cache.conversationTemplates = await cache.nonCachedDatabase.GetConversationTemplatesAsync();

                    progress.Report(5, steps, "Loading gossip menus");
                    cache.gossipMenusCache = await cache.nonCachedDatabase.GetGossipMenusAsync();

                    progress.Report(6, steps, "Loading npc texts");
                    cache.npcTextsCache = await cache.nonCachedDatabase.GetNpcTextsAsync();
                    cache.npcTextsByIdCache = cache.npcTextsCache.ToDictionary(npcText => npcText.Id);

                    progress.Report(7, steps, "Loading quests");
                    cache.questTemplateCache = await cache.nonCachedDatabase.GetQuestTemplatesAsync();

                    progress.Report(8, steps, "Loading creature class level stats");
                    cache.creatureClassLevelStatsCache =
                        await cache.nonCachedDatabase.GetCreatureClassLevelStatsAsync();

                    progress.Report(9, steps, "Loading database spell dbc");
                    cache.databaseSpellDbcCache = await cache.nonCachedDatabase.GetSpellDbcAsync();
                    
                    progress.Report(10, steps, "Loading broadcast texts");
                    var broadcastTexts = await cache.nonCachedDatabase.GetBroadcastTextsAsync();
                    cache.broadcastTextsSortedCache = broadcastTexts;
                    cache.broadcastTextIndexByIdCache = broadcastTexts.Select((t, i) => (t.Id, i)).ToDictionary(t => t.Id, t => t.i);
                    // to expensive to cache
                    // var cachedTrie = new StringTrie<IBroadcastText>();
                    // await Task.Run(() =>
                    // {
                    //     foreach (var text in broadcastTexts)
                    //     {
                    //         if (text.Text != null)
                    //             cachedTrie[text.Text] = text;
                    //         if (text.Text1 != null)
                    //             cachedTrie[text.Text1] = text;
                    //     }
                    // }).ConfigureAwait(true);
                    //cache.broadcastTextsCache = cachedTrie;

                    progress.Report(11, steps, "Loading creature spawns");
                    var creatures = await cache.nonCachedDatabase.GetCreaturesAsync();

                    progress.Report(12, steps, "Loading gameobject spawns");
                    var gameObjects = await cache.nonCachedDatabase.GetGameObjectsAsync();

                    progress.Report(13, steps, "Loading scene templates");
                    var sceneTemplates = await cache.nonCachedDatabase.GetSceneTemplatesAsync();

                    Dictionary<uint, ICreatureTemplate> creatureTemplateByEntry = new();
                    Dictionary<uint, IGameObjectTemplate> gameObjectTemplateByEntry = new();
                    Dictionary<uint, IQuestTemplate> questTemplateByEntry = new();
                    Dictionary<uint, ICreatureModelInfo> creatureModelInfos = new();
                    Dictionary<(uint, uint), ICreature> creaturesByGuid = new();
                    Dictionary<(uint, uint), IGameObject> gameObjectsByGuid = new();
                    Dictionary<uint, ISceneTemplate> sceneByEntry = new();

                    foreach (var entity in cache.creatureTemplateCache)
                        creatureTemplateByEntry[entity.Entry] = entity;

                    foreach (var entity in cache.gameObjectTemplateCache)
                        gameObjectTemplateByEntry[entity.Entry] = entity;

                    foreach (var entity in cache.questTemplateCache)
                        questTemplateByEntry[entity.Entry] = entity;

                    foreach (var entity in (await cache.nonCachedDatabase.GetCreatureModelInfoAsync()))
                        creatureModelInfos[entity.DisplayId] = entity;

                    foreach (var spawn in creatures)
                    {
                        creaturesByGuid[(spawn.Entry, spawn.Guid)] = spawn;
                        creaturesByGuid[(0, spawn.Guid)] = spawn;
                    }

                    foreach (var spawn in gameObjects)
                    {
                        gameObjectsByGuid[(spawn.Entry, spawn.Guid)] = spawn;
                        gameObjectsByGuid[(0, spawn.Guid)] = spawn;
                    }

                    if (sceneTemplates != null)
                        foreach (var entity in sceneTemplates)
                            sceneByEntry[entity.SceneId] = entity;

                    cache.creatureTemplateByEntry = creatureTemplateByEntry;
                    cache.gameObjectTemplateByEntry = gameObjectTemplateByEntry;
                    cache.questTemplateByEntry = questTemplateByEntry;
                    cache.creatureModelInfos = creatureModelInfos;
                    cache.creaturesByGuid = creaturesByGuid;
                    cache.gameObjectsByGuid = gameObjectsByGuid;
                    cache.sceneByEntry = sceneByEntry;
                }
                catch (Exception e)
                {
                    cache.nonCachedDatabase = new NullWorldDatabaseProvider();
                    cache.statusBar.PublishNotification(new PlainNotification(NotificationType.Error,
                        $"Error while connecting to the database. Make sure you have correct core version in the settings: {e.Message}"));
                    throw;
                }
                finally
                {
                    cache.cacheInProgress = false;
                    cache.cacheTask = Task.CompletedTask;
                    cache.loadingEventAggregator.Publish<DatabaseLoadedEvent>();
                }
            }
        }
    }
}
