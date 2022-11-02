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
    public class CachedDatabaseProvider : IDatabaseProvider
    {
        private IReadOnlyList<ICreatureTemplate>? creatureTemplateCache;
        private Dictionary<uint, ICreatureTemplate> creatureTemplateByEntry = new();
        
        private List<IGameObjectTemplate>? gameObjectTemplateCache;
        private Dictionary<uint, IGameObjectTemplate> gameObjectTemplateByEntry = new();

        private List<IQuestTemplate>? questTemplateCache;
        private Dictionary<uint, IQuestTemplate> questTemplateByEntry = new();
        
        private Dictionary<uint, ICreatureModelInfo>? creatureModelInfos;
        
        private Dictionary<uint, IReadOnlyList<ICreatureText>?> creatureTextsCache = new();

        private List<IAreaTriggerTemplate>? areaTriggerTemplates;
        private List<IGameEvent>? gameEventsCache;
        private List<IConversationTemplate>? conversationTemplates;
        private List<IGossipMenu>? gossipMenusCache;
        private List<INpcText>? npcTextsCache;
        private Dictionary<uint, INpcText>? npcTextsByIdCache;
        private List<ICreatureClassLevelStat>? creatureClassLevelStatsCache;
        private IList<IDatabaseSpellDbc>? databaseSpellDbcCache;

        private StringTrie<IBroadcastText>? broadcastTextsCache;
        
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
                if (tableName == "creature_template")
                    Refresh(RefreshCreatureTemplate);
                //if (tableName == "creature")
                //    Refresh(RefreshCreature);
                else if (tableName == "gossip_menu")
                    Refresh(RefreshGossipMenu);
                else if (tableName == "npc_text")
                    Refresh(RefreshNpcTexts);
                else if (tableName == creatureTextInsertProvider.TableName)
                    Refresh(RefreshCreatureTexts);
                else if (tableName == "quest_template" || tableName == "quest_template_addon")
                    Refresh(RefreshQuestTemplates);
            }, true);
        }

        private void Refresh(Func<Task<System.Type>> refreshingFunc)
        {
            taskRunner.ScheduleTask("Refresh database", async () =>
                {
                    var type = await refreshingFunc();
                    eventAggregator.GetEvent<DatabaseCacheReloaded>().Publish(type);
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
            var templates = await nonCachedDatabase.GetCreatureTemplatesAsync().ConfigureAwait(false);
            Dictionary<uint, ICreatureTemplate> tempDict = templates.ToDictionary(t => t.Entry);
            creatureTemplateCache = templates;
            creatureTemplateByEntry = tempDict;
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
        
        public Task TryConnect()
        {
            nonCachedDatabase.GetCreatureTemplate(0); // if there is some connection problem, it should throw
            return taskRunner.ScheduleTask(new DatabaseCacheTask(this));
        }

        public bool IsConnected => nonCachedDatabase.IsConnected;

        public ICreatureTemplate? GetCreatureTemplate(uint entry)
        {
            if (entry == 0)
                return null;

            if (creatureTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return nonCachedDatabase.GetCreatureTemplate(entry);
        }

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
            if (entry == 0)
                return null;
            
            if (gameObjectTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return nonCachedDatabase.GetGameObjectTemplate(entry);
        }

        public IQuestTemplate? GetQuestTemplate(uint entry)
        {
            if (questTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return nonCachedDatabase.GetQuestTemplate(entry);
        }

        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates()
        {
            if (gameObjectTemplateCache != null)
                return gameObjectTemplateCache;

            return nonCachedDatabase.GetGameObjectTemplates();
        }

        public Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry) => nonCachedDatabase.GetAreaTriggerScript(entry);

        public IEnumerable<ICreatureTemplate> GetCreatureTemplates()
        {
            if (creatureTemplateCache != null)
                return creatureTemplateCache;

            return nonCachedDatabase.GetCreatureTemplates();
        }

        public Task<IList<IGameObject>> GetGameObjectsByEntryAsync(uint entry)
        {
            return nonCachedDatabase.GetGameObjectsByEntryAsync(entry);
        }

        public IEnumerable<ICreature> GetCreatures()
        {
            return nonCachedDatabase.GetCreatures();
        }

        public Task<IList<ICreature>> GetCreaturesAsync() => ((IDatabaseProvider)nonCachedDatabase).GetCreaturesAsync();

        public Task<IList<IGameObject>> GetGameObjectsAsync() => ((IDatabaseProvider)nonCachedDatabase).GetGameObjectsAsync();

        public Task<IList<ICreature>> GetCreaturesByMapAsync(uint map) => nonCachedDatabase.GetCreaturesByMapAsync(map);
        
        public Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map) => nonCachedDatabase.GetGameObjectsByMapAsync(map);

        public IEnumerable<IGameObject> GetGameObjects()
        {
            return nonCachedDatabase.GetGameObjects();
        }

        public IEnumerable<IQuestTemplate> GetQuestTemplates()
        {
            if (questTemplateCache != null)
                return questTemplateCache;

            return nonCachedDatabase.GetQuestTemplates();
        }

        public async Task<IList<IQuestObjective>> GetQuestObjectives(uint questId) => await nonCachedDatabase.GetQuestObjectives(questId);

        public async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => await nonCachedDatabase.GetQuestObjective(questId, storageIndex);

        public async Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId) => await nonCachedDatabase.GetQuestObjectiveById(objectiveId);

        public Task<IQuestRequestItem?> GetQuestRequestItem(uint entry) => nonCachedDatabase.GetQuestRequestItem(entry);

        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats() =>
            creatureClassLevelStatsCache ?? nonCachedDatabase.GetCreatureClassLevelStats();

        public IEnumerable<IGameEvent> GetGameEvents() => gameEventsCache ?? nonCachedDatabase.GetGameEvents();
        
        public IEnumerable<IConversationTemplate> GetConversationTemplates() => conversationTemplates ?? nonCachedDatabase.GetConversationTemplates();

        public IEnumerable<IGossipMenu> GetGossipMenus() => gossipMenusCache ?? nonCachedDatabase.GetGossipMenus();

        public Task<List<IGossipMenu>> GetGossipMenusAsync() => gossipMenusCache == null
            ? nonCachedDatabase.GetGossipMenusAsync()
            : Task.FromResult(gossipMenusCache);

        public Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId) => nonCachedDatabase.GetGossipMenuOptionsAsync(menuId);
        public List<IGossipMenuOption> GetGossipMenuOptions(uint menuId) => nonCachedDatabase.GetGossipMenuOptions(menuId);

        public IEnumerable<INpcText> GetNpcTexts() => npcTextsCache ?? nonCachedDatabase.GetNpcTexts();

        public INpcText? GetNpcText(uint entry)
        {
            if (npcTextsByIdCache == null)
                return nonCachedDatabase.GetNpcText(entry);
            npcTextsByIdCache.TryGetValue(entry, out var val);
            return val;
        }

        public Task<List<IPointOfInterest>> GetPointsOfInterestsAsync() => nonCachedDatabase.GetPointsOfInterestsAsync();

        public Task<List<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry) => nonCachedDatabase.GetCreatureTextsByEntryAsync(entry);

        public Task<IList<IItem>?> GetItemTemplatesAsync() => nonCachedDatabase.GetItemTemplatesAsync();
        public Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions) => nonCachedDatabase.FindSmartScriptLinesBy(conditions);

        public Task<IList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync() => nonCachedDatabase.GetSpawnGroupTemplatesAsync();
        public Task<IList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync() => nonCachedDatabase.GetSpawnGroupSpawnsAsync();
        public Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id) => nonCachedDatabase.GetSpawnGroupTemplateByIdAsync(id);
        public Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type) => nonCachedDatabase.GetSpawnGroupSpawnByGuidAsync(guid, type);
        public Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id) => nonCachedDatabase.GetSpawnGroupFormation(id);
        public Task<IList<ISpawnGroupFormation>?> GetSpawnGroupFormations() => nonCachedDatabase.GetSpawnGroupFormations();
        
        public IReadOnlyList<ICreatureText>? GetCreatureTextsByEntry(uint entry)
        {
            if (creatureTextsCache.TryGetValue(entry, out var texts))
                return texts;

            creatureTextsCache[entry] = texts = nonCachedDatabase.GetCreatureTextsByEntry(entry);
            return texts;
        }

        public Task<IList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList) => nonCachedDatabase.GetLinesCallingSmartTimedActionList(timedActionList);

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates() =>
            areaTriggerTemplates ?? nonCachedDatabase.GetAreaTriggerTemplates();

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            return nonCachedDatabase.GetScriptFor(entryOrGuid, type);
        }

        public async Task<IList<ISmartScriptLine>> GetScriptForAsync(int entryOrGuid, SmartScriptType type) => await nonCachedDatabase.GetScriptForAsync(entryOrGuid, type);

        public async Task InstallConditions(IEnumerable<IConditionLine> conditions,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null)
        {
            await nonCachedDatabase.InstallConditions(conditions, keyMask, manualKey);
        }

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId)
        {
            return nonCachedDatabase.GetConditionsFor(sourceType, sourceEntry, sourceId);
        }
        
        public Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key)
        {
            return nonCachedDatabase.GetConditionsForAsync(keyMask, key);
        }

        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId)
        {
            return nonCachedDatabase.GetSpellScriptNames(spellId);
        }

        public Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            nonCachedDatabase.GetSmartScriptEntriesByType(scriptType);

        public async Task<IList<IPlayerChoice>?> GetPlayerChoicesAsync() => await nonCachedDatabase.GetPlayerChoicesAsync();

        public async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => await nonCachedDatabase.GetPlayerChoiceResponsesAsync();

        public async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => await nonCachedDatabase.GetPlayerChoiceResponsesAsync(choiceId);

        public IEnumerable<ISmartScriptProjectItem> GetLegacyProjectItems() => nonCachedDatabase.GetLegacyProjectItems();
        
        public IEnumerable<ISmartScriptProject> GetLegacyProjects() => nonCachedDatabase.GetLegacyProjects();

        public IBroadcastText? GetBroadcastTextByText(string text)
        {
            if (broadcastTextsCache == null)
                return nonCachedDatabase.GetBroadcastTextByText(text);
            if (broadcastTextsCache.TryGetValue(text, out var val))
                return val;
            return null;
        }

        public async Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text)
        {
            if (broadcastTextsCache == null)
                return await nonCachedDatabase.GetBroadcastTextByTextAsync(text);
            if (broadcastTextsCache.TryGetValue(text, out var val))
                return val;
            return null;
        }

        public Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id) => nonCachedDatabase.GetBroadcastTextByIdAsync(id);
        public Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text) => nonCachedDatabase.GetBroadcastTextLocaleByTextAsync(text);

        public Task<List<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions) => nonCachedDatabase.FindEventScriptLinesBy(conditions);
        public Task<IList<ICreatureModelInfo>> GetCreatureModelInfoAsync() => nonCachedDatabase.GetCreatureModelInfoAsync();

        public ICreatureModelInfo? GetCreatureModelInfo(uint displayId)
        {
            if (creatureModelInfos == null)
                return nonCachedDatabase.GetCreatureModelInfo(displayId);
            if (creatureModelInfos.TryGetValue(displayId, out var info))
                return info;
            return null;
        }

        public ISceneTemplate? GetSceneTemplate(uint sceneId) => nonCachedDatabase.GetSceneTemplate(sceneId);

        public Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId) => nonCachedDatabase.GetSceneTemplateAsync(sceneId);

        public Task<IList<ISceneTemplate>?> GetSceneTemplatesAsync() => nonCachedDatabase.GetSceneTemplatesAsync();
        public Task<IList<ICreatureAddon>> GetCreatureAddons() => nonCachedDatabase.GetCreatureAddons();

        public Task<IList<ICreatureTemplateAddon>> GetCreatureTemplateAddons() => nonCachedDatabase.GetCreatureTemplateAddons();

        public Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates() => nonCachedDatabase.GetCreatureEquipmentTemplates();

        public Task<IList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates() => nonCachedDatabase.GetMangosCreatureEquipmentTemplates();

        public Task<IList<IGameEventCreature>> GetGameEventCreaturesAsync() => nonCachedDatabase.GetGameEventCreaturesAsync();

        public Task<IList<IGameEventGameObject>> GetGameEventGameObjectsAsync() => nonCachedDatabase.GetGameEventGameObjectsAsync();

        public Task<IList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint guid) => nonCachedDatabase.GetGameEventCreaturesByGuidAsync(guid);

        public Task<IList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint guid) => nonCachedDatabase.GetGameEventGameObjectsByGuidAsync(guid);

        public Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry) => nonCachedDatabase.GetCreatureEquipmentTemplates(entry);

        public Task<IGameObject?> GetGameObjectByGuidAsync(uint guid) => nonCachedDatabase.GetGameObjectByGuidAsync(guid);

        public Task<ICreature?> GetCreaturesByGuidAsync(uint guid) => nonCachedDatabase.GetCreaturesByGuidAsync(guid);

        public Task<ICreatureAddon?> GetCreatureAddon(uint guid) => nonCachedDatabase.GetCreatureAddon(guid);

        public Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry) => nonCachedDatabase.GetCreatureTemplateAddon(entry);
       
        public Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId) => nonCachedDatabase.GetWaypointData(pathId);

        public Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId) => nonCachedDatabase.GetSmartScriptWaypoints(pathId);

        public Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId) => nonCachedDatabase.GetScriptWaypoints(pathId);

        public Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId) => nonCachedDatabase.GetMangosWaypoints(pathId);

        public Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid) => nonCachedDatabase.GetMangosCreatureMovement(guid);
        
        public Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId) => nonCachedDatabase.GetMangosCreatureMovementTemplate(entry, pathId);
        
        public Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId) => nonCachedDatabase.GetMangosPathName(pathId);

        public Task<List<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => nonCachedDatabase.GetEventScript(type, id);

        public Task<List<IEventAiLine>> GetEventAi(int id) => nonCachedDatabase.GetEventAi(id);

        public Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync()
        {
            if (databaseSpellDbcCache != null)
                return Task.FromResult(databaseSpellDbcCache);

            return nonCachedDatabase.GetSpellDbcAsync();
        }
        
        public ICreature? GetCreatureByGuid(uint guid) => nonCachedDatabase.GetCreatureByGuid(guid);

        public IGameObject? GetGameObjectByGuid(uint guid) => nonCachedDatabase.GetGameObjectByGuid(guid);
        
        public IEnumerable<ICreature> GetCreaturesByEntry(uint entry) => nonCachedDatabase.GetCreaturesByEntry(entry);

        public IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry) => nonCachedDatabase.GetGameObjectsByEntry(entry);
        
        public Task<IList<ICreature>> GetCreaturesByEntryAsync(uint entry) => nonCachedDatabase.GetCreaturesByEntryAsync(entry);

        public IEnumerable<ICoreCommandHelp> GetCommands() => nonCachedDatabase.GetCommands();

        public Task<IList<ITrinityString>> GetStringsAsync() => nonCachedDatabase.GetStringsAsync();

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
                    int steps = 11;

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
                    // todo: is there any benefit of caching this?
                    /*var broadcastTexts = await cache.nonCachedDatabase.GetBroadcastTextsAsync();
                    var cachedTrie = new StringTrie<IBroadcastText>();
                    await Task.Run(() =>
                    {
                        foreach (var text in broadcastTexts)
                        {
                            if (text.Text != null)
                                cachedTrie[text.Text] = text;
                            if (text.Text1 != null)
                                cachedTrie[text.Text1] = text;
                        }
                    }).ConfigureAwait(true);
                    cache.broadcastTextsCache = cachedTrie;*/

                    Dictionary<uint, ICreatureTemplate> creatureTemplateByEntry = new();
                    Dictionary<uint, IGameObjectTemplate> gameObjectTemplateByEntry = new();
                    Dictionary<uint, IQuestTemplate> questTemplateByEntry = new();
                    Dictionary<uint, ICreatureModelInfo> creatureModelInfos = new();

                    foreach (var entity in cache.creatureTemplateCache)
                        creatureTemplateByEntry[entity.Entry] = entity;

                    foreach (var entity in cache.gameObjectTemplateCache)
                        gameObjectTemplateByEntry[entity.Entry] = entity;

                    foreach (var entity in cache.questTemplateCache)
                        questTemplateByEntry[entity.Entry] = entity;

                    foreach (var entity in (await cache.nonCachedDatabase.GetCreatureModelInfoAsync()))
                        creatureModelInfos[entity.DisplayId] = entity;
                    
                    cache.creatureTemplateByEntry = creatureTemplateByEntry;
                    cache.gameObjectTemplateByEntry = gameObjectTemplateByEntry;
                    cache.questTemplateByEntry = questTemplateByEntry;
                    cache.creatureModelInfos = creatureModelInfos;
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
                    cache.loadingEventAggregator.Publish<DatabaseLoadedEvent>();
                }
            }
        }
    }
}