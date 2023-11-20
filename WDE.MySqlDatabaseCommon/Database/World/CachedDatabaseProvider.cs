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
        private IReadOnlyList<ICreatureTemplate>? creatureTemplateCache;
        private Dictionary<uint, ICreatureTemplate> creatureTemplateByEntry = new();
        
        private List<IGameObjectTemplate>? gameObjectTemplateCache;
        private Dictionary<uint, IGameObjectTemplate> gameObjectTemplateByEntry = new();

        private List<IQuestTemplate>? questTemplateCache;
        private Dictionary<uint, IQuestTemplate> questTemplateByEntry = new();
        
        private Dictionary<uint, ICreatureModelInfo>? creatureModelInfos;
        
        private Dictionary<uint, IReadOnlyList<ICreatureText>?> creatureTextsCache = new();

        private IList<IAreaTriggerTemplate>? areaTriggerTemplates;
        private List<IGameEvent>? gameEventsCache;
        private List<IConversationTemplate>? conversationTemplates;
        private List<IGossipMenu>? gossipMenusCache;
        private List<INpcText>? npcTextsCache;
        private Dictionary<uint, INpcText>? npcTextsByIdCache;
        private List<ICreatureClassLevelStat>? creatureClassLevelStatsCache;
        private IList<IDatabaseSpellDbc>? databaseSpellDbcCache;

        private List<IBroadcastText>? broadcastTextsSortedCache;
        private Dictionary<uint, int>? broadcastTextIndexByIdCache;
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

        public IReadOnlyList<IGameObjectTemplate> GetGameObjectTemplates()
        {
            if (gameObjectTemplateCache != null)
                return gameObjectTemplateCache;

            return nonCachedDatabase.GetGameObjectTemplates();
        }

        public Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry) => WaitForCache(nonCachedDatabase.GetAreaTriggerScript(entry));
        
        public Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry) => WaitForCache(nonCachedDatabase.GetAreaTriggerTemplate(entry));

        public IReadOnlyList<ICreatureTemplate> GetCreatureTemplates()
        {
            if (creatureTemplateCache != null)
                return creatureTemplateCache;

            return nonCachedDatabase.GetCreatureTemplates();
        }

        public async Task<IList<IGameObject>> GetGameObjectsByEntryAsync(uint entry)
        {
            await WaitForCache();
            return await nonCachedDatabase.GetGameObjectsByEntryAsync(entry);
        }

        public IReadOnlyList<ICreature> GetCreatures()
        {
            return nonCachedDatabase.GetCreatures();
        }

        public Task<IList<ICreature>> GetCreaturesAsync() => WaitForCache(nonCachedDatabase.GetCreaturesAsync());

        public Task<IList<IGameObject>> GetGameObjectsAsync() => WaitForCache(nonCachedDatabase.GetGameObjectsAsync());

        public Task<IList<ICreature>> GetCreaturesByMapAsync(uint map) => WaitForCache(nonCachedDatabase.GetCreaturesByMapAsync(map));
        
        public Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map) => WaitForCache(nonCachedDatabase.GetGameObjectsByMapAsync(map));

        public IEnumerable<IGameObject> GetGameObjects()
        {
            return nonCachedDatabase.GetGameObjects();
        }

        public IReadOnlyList<IQuestTemplate> GetQuestTemplates()
        {
            if (questTemplateCache != null)
                return questTemplateCache;

            return nonCachedDatabase.GetQuestTemplates();
        }

        public Task<IList<IQuestObjective>> GetQuestObjectives(uint questId) => WaitForCache(nonCachedDatabase.GetQuestObjectives(questId));

        public Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => WaitForCache(nonCachedDatabase.GetQuestObjective(questId, storageIndex));

        public Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId) => WaitForCache(nonCachedDatabase.GetQuestObjectiveById(objectiveId));

        public Task<IQuestRequestItem?> GetQuestRequestItem(uint entry) => WaitForCache(nonCachedDatabase.GetQuestRequestItem(entry));

        public async Task<IList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync()
        {
            await WaitForCache();
            return areaTriggerTemplates ?? await nonCachedDatabase.GetAreaTriggerTemplatesAsync();
        }

        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats() =>
            creatureClassLevelStatsCache ?? nonCachedDatabase.GetCreatureClassLevelStats();

        public IEnumerable<IGameEvent> GetGameEvents() => gameEventsCache ?? nonCachedDatabase.GetGameEvents();
        
        public IEnumerable<IConversationTemplate> GetConversationTemplates() => conversationTemplates ?? nonCachedDatabase.GetConversationTemplates();

        public IEnumerable<IGossipMenu> GetGossipMenus() => gossipMenusCache ?? nonCachedDatabase.GetGossipMenus();

        public Task<List<IGossipMenu>> GetGossipMenusAsync() => gossipMenusCache == null
            ? WaitForCache(nonCachedDatabase.GetGossipMenusAsync())
            : Task.FromResult(gossipMenusCache);

        public Task<IGossipMenu?> GetGossipMenuAsync(uint menuId) => WaitForCache(nonCachedDatabase.GetGossipMenuAsync(menuId));

        public Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId) => WaitForCache(nonCachedDatabase.GetGossipMenuOptionsAsync(menuId));
        public List<IGossipMenuOption> GetGossipMenuOptions(uint menuId) => nonCachedDatabase.GetGossipMenuOptions(menuId);

        public IEnumerable<INpcText> GetNpcTexts() => npcTextsCache ?? nonCachedDatabase.GetNpcTexts();

        public INpcText? GetNpcText(uint entry)
        {
            if (npcTextsByIdCache == null)
                return nonCachedDatabase.GetNpcText(entry);
            npcTextsByIdCache.TryGetValue(entry, out var val);
            return val;
        }

        public Task<List<IPointOfInterest>> GetPointsOfInterestsAsync() => WaitForCache(nonCachedDatabase.GetPointsOfInterestsAsync());
        
        public async Task<List<IBroadcastText>> GetBroadcastTextsAsync()
        {
            await WaitForCache();
            return broadcastTextsSortedCache ?? await nonCachedDatabase.GetBroadcastTextsAsync();
        }

        public Task<List<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry) => WaitForCache(nonCachedDatabase.GetCreatureTextsByEntryAsync(entry));

        public Task<IList<IItem>?> GetItemTemplatesAsync() => WaitForCache(nonCachedDatabase.GetItemTemplatesAsync());
        public Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions) 
            => WaitForCache(nonCachedDatabase.FindSmartScriptLinesBy(conditions));

        public Task<IList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync() => WaitForCache(nonCachedDatabase.GetSpawnGroupTemplatesAsync());
        public Task<IList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync() => WaitForCache(nonCachedDatabase.GetSpawnGroupSpawnsAsync());
        public Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id) => WaitForCache(nonCachedDatabase.GetSpawnGroupTemplateByIdAsync(id));
        public Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type) => WaitForCache(nonCachedDatabase.GetSpawnGroupSpawnByGuidAsync(guid, type));
        public Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id) => WaitForCache(nonCachedDatabase.GetSpawnGroupFormation(id));
        public Task<IList<ISpawnGroupFormation>?> GetSpawnGroupFormations() => WaitForCache(nonCachedDatabase.GetSpawnGroupFormations());
        
        public IReadOnlyList<ICreatureText>? GetCreatureTextsByEntry(uint entry)
        {
            if (creatureTextsCache.TryGetValue(entry, out var texts))
                return texts;

            creatureTextsCache[entry] = texts = nonCachedDatabase.GetCreatureTextsByEntry(entry);
            return texts;
        }

        public Task<IList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList) => WaitForCache(nonCachedDatabase.GetLinesCallingSmartTimedActionList(timedActionList));

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates() =>
            areaTriggerTemplates ?? nonCachedDatabase.GetAreaTriggerTemplates();

        public IEnumerable<ISmartScriptLine> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type)
        {
            return nonCachedDatabase.GetScriptFor(entry, entryOrGuid, type);
        }

        public Task<IList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type) 
            => WaitForCache(nonCachedDatabase.GetScriptForAsync(entry, entryOrGuid, type));

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId)
        {
            return nonCachedDatabase.GetConditionsFor(sourceType, sourceEntry, sourceId);
        }
        
        public async Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key)
        {
            await WaitForCache();
            return await nonCachedDatabase.GetConditionsForAsync(keyMask, key);
        }

        public async Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys)
        {
            await WaitForCache();
            return await nonCachedDatabase.GetConditionsForAsync(keyMask, manualKeys);
        }

        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId)
        {
            return nonCachedDatabase.GetSpellScriptNames(spellId);
        }

        public Task<IReadOnlyList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            WaitForCache(nonCachedDatabase.GetSmartScriptEntriesByType(scriptType));

        public Task<IList<IPlayerChoice>?> GetPlayerChoicesAsync() => WaitForCache(nonCachedDatabase.GetPlayerChoicesAsync());

        public Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => WaitForCache(nonCachedDatabase.GetPlayerChoiceResponsesAsync());

        public Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => WaitForCache(nonCachedDatabase.GetPlayerChoiceResponsesAsync(choiceId));

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

        public Task<List<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions) 
            => WaitForCache(nonCachedDatabase.FindEventScriptLinesBy(conditions));
        public Task<IList<ICreatureModelInfo>> GetCreatureModelInfoAsync() 
            => WaitForCache(nonCachedDatabase.GetCreatureModelInfoAsync());

        public ICreatureModelInfo? GetCreatureModelInfo(uint displayId)
        {
            if (creatureModelInfos == null)
                return nonCachedDatabase.GetCreatureModelInfo(displayId);
            if (creatureModelInfos.TryGetValue(displayId, out var info))
                return info;
            return null;
        }

        public ISceneTemplate? GetSceneTemplate(uint sceneId) => nonCachedDatabase.GetSceneTemplate(sceneId);

        public Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId) => WaitForCache(nonCachedDatabase.GetSceneTemplateAsync(sceneId));

        public Task<IList<ISceneTemplate>?> GetSceneTemplatesAsync() => WaitForCache(nonCachedDatabase.GetSceneTemplatesAsync());

        public Task<IPhaseName?> GetPhaseNameAsync(uint phaseId) => WaitForCache(nonCachedDatabase.GetPhaseNameAsync(phaseId));

        public Task<IList<IPhaseName>?> GetPhaseNamesAsync() => WaitForCache(nonCachedDatabase.GetPhaseNamesAsync());

        public Task<IReadOnlyList<INpcSpellClickSpell>> GetNpcSpellClickSpells(uint creatureId) => nonCachedDatabase.GetNpcSpellClickSpells(creatureId);

        public IList<IPhaseName>? GetPhaseNames() => nonCachedDatabase.GetPhaseNames();

        public Task<IList<ICreatureAddon>> GetCreatureAddons() => WaitForCache(nonCachedDatabase.GetCreatureAddons());

        public Task<IList<ICreatureTemplateAddon>> GetCreatureTemplateAddons() => WaitForCache(nonCachedDatabase.GetCreatureTemplateAddons());

        public Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates() => WaitForCache(nonCachedDatabase.GetCreatureEquipmentTemplates());

        public Task<IList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates() => WaitForCache(nonCachedDatabase.GetMangosCreatureEquipmentTemplates());

        public Task<IList<IGameEventCreature>> GetGameEventCreaturesAsync() => WaitForCache(nonCachedDatabase.GetGameEventCreaturesAsync());

        public Task<IList<IGameEventGameObject>> GetGameEventGameObjectsAsync() => WaitForCache(nonCachedDatabase.GetGameEventGameObjectsAsync());

        public Task<IList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetGameEventCreaturesByGuidAsync(entry, guid));

        public Task<IList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetGameEventGameObjectsByGuidAsync(entry, guid));

        public Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry) => WaitForCache(nonCachedDatabase.GetCreatureEquipmentTemplates(entry));

        public Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetGameObjectByGuidAsync(entry, guid));

        public Task<ICreature?> GetCreaturesByGuidAsync(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetCreaturesByGuidAsync(entry, guid));

        public Task<ICreatureAddon?> GetCreatureAddon(uint entry, uint guid) => WaitForCache(nonCachedDatabase.GetCreatureAddon(entry, guid));

        public Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry) => WaitForCache(nonCachedDatabase.GetCreatureTemplateAddon(entry));
       
        public Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId) => WaitForCache(nonCachedDatabase.GetWaypointData(pathId));

        public Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId) => WaitForCache(nonCachedDatabase.GetSmartScriptWaypoints(pathId));

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

        public Task<List<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => WaitForCache(nonCachedDatabase.GetEventScript(type, id));

        public Task<List<IEventAiLine>> GetEventAi(int id) => WaitForCache(nonCachedDatabase.GetEventAi(id));

        public async Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync()
        {
            await WaitForCache();
            
            if (databaseSpellDbcCache != null)
                return databaseSpellDbcCache;

            return await nonCachedDatabase.GetSpellDbcAsync();
        }
        
        public Task<IList<IDatabaseSpellEffectDbc>> GetSpellEffectDbcAsync() => WaitForCache(nonCachedDatabase.GetSpellEffectDbcAsync());

        public ICreature? GetCreatureByGuid(uint entry, uint guid) => nonCachedDatabase.GetCreatureByGuid(entry, guid);

        public IGameObject? GetGameObjectByGuid(uint entry, uint guid) => nonCachedDatabase.GetGameObjectByGuid(entry, guid);
        
        public IEnumerable<ICreature> GetCreaturesByEntry(uint entry) => nonCachedDatabase.GetCreaturesByEntry(entry);

        public IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry) => nonCachedDatabase.GetGameObjectsByEntry(entry);
        
        public Task<IList<ICreature>> GetCreaturesByEntryAsync(uint entry) => WaitForCache(nonCachedDatabase.GetCreaturesByEntryAsync(entry));

        public IEnumerable<ICoreCommandHelp> GetCommands() => nonCachedDatabase.GetCommands();

        public Task<IList<ITrinityString>> GetStringsAsync() => WaitForCache(nonCachedDatabase.GetStringsAsync());

        public Task<IReadOnlyList<IQuestScriptName>> GetQuestScriptNames(uint questId) => WaitForCache(nonCachedDatabase.GetQuestScriptNames(questId));

        private bool cacheInProgress = false;
        private Task cacheTask = Task.CompletedTask;

        public async Task WaitForCache()
        {
            if (!cacheInProgress)
                return;

            await cacheTask;
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
                    cache.cacheInProgress = false;
                    cache.cacheTask = Task.CompletedTask;
                    cache.loadingEventAggregator.Publish<DatabaseLoadedEvent>();
                }
            }
        }
    }
}