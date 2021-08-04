using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KTrie;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Tasks;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public class CachedDatabaseProvider : IDatabaseProvider
    {
        private List<ICreatureTemplate>? creatureTemplateCache;
        private Dictionary<uint, ICreatureTemplate> creatureTemplateByEntry = new();

        private List<IGameObjectTemplate>? gameObjectTemplateCache;
        private Dictionary<uint, IGameObjectTemplate> gameObjectTemplateByEntry = new();

        private List<IQuestTemplate>? questTemplateCache;
        private Dictionary<uint, IQuestTemplate> questTemplateByEntry = new();

        private List<IAreaTriggerTemplate>? areaTriggerTemplates;
        private List<IGameEvent>? gameEventsCache;
        private List<IConversationTemplate>? conversationTemplates;
        private List<IGossipMenu>? gossipMenusCache;
        private List<INpcText>? npcTextsCache;
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
            IParameterFactory parameterFactory)
        {
            this.nonCachedDatabase = nonCachedDatabase;
            this.taskRunner = taskRunner;
            this.statusBar = statusBar;
            this.eventAggregator = eventAggregator;
            this.loadingEventAggregator = loadingEventAggregator;
            this.parameterFactory = parameterFactory;
        }

        public Task TryConnect()
        {
            return taskRunner.ScheduleTask(new DatabaseCacheTask(this));;
        }

        public bool IsConnected => nonCachedDatabase.IsConnected;

        public ICreatureTemplate? GetCreatureTemplate(uint entry)
        {
            if (creatureTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return null;//nonCachedDatabase.GetCreatureTemplate(entry);
        }

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
            if (gameObjectTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return null;//nonCachedDatabase.GetGameObjectTemplate(entry);
        }

        public IQuestTemplate? GetQuestTemplate(uint entry)
        {
            if (questTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return null;//nonCachedDatabase.GetQuestTemplate(entry);
        }

        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates()
        {
            if (gameObjectTemplateCache != null)
                return gameObjectTemplateCache;

            return nonCachedDatabase.GetGameObjectTemplates();
        }

        public IEnumerable<ICreatureTemplate> GetCreatureTemplates()
        {
            if (creatureTemplateCache != null)
                return creatureTemplateCache;

            return nonCachedDatabase.GetCreatureTemplates();
        }

        public IEnumerable<IQuestTemplate> GetQuestTemplates()
        {
            if (questTemplateCache != null)
                return questTemplateCache;

            return nonCachedDatabase.GetQuestTemplates();
        }

        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats() =>
            creatureClassLevelStatsCache ?? nonCachedDatabase.GetCreatureClassLevelStats();

        public IEnumerable<IGameEvent> GetGameEvents() => gameEventsCache ?? nonCachedDatabase.GetGameEvents();
        
        public IEnumerable<IConversationTemplate> GetConversationTemplates() => conversationTemplates ?? nonCachedDatabase.GetConversationTemplates();

        public IEnumerable<IGossipMenu> GetGossipMenus() => gossipMenusCache ?? nonCachedDatabase.GetGossipMenus();
        
        public IEnumerable<INpcText> GetNpcTexts() => npcTextsCache ?? nonCachedDatabase.GetNpcTexts();
        
        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates() =>
            areaTriggerTemplates ?? nonCachedDatabase.GetAreaTriggerTemplates();

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            return nonCachedDatabase.GetScriptFor(entryOrGuid, type);
        }

        public async Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script)
        {
            await nonCachedDatabase.InstallScriptFor(entryOrGuid, type, script);
        }

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
        
        public IEnumerable<ISmartScriptProjectItem> GetProjectItems() => nonCachedDatabase.GetProjectItems();
        
        public IEnumerable<ISmartScriptProject> GetProjects() => nonCachedDatabase.GetProjects();

        public IBroadcastText? GetBroadcastTextByText(string text)
        {
            if (broadcastTextsCache == null)
                return nonCachedDatabase.GetBroadcastTextByText(text);
            if (broadcastTextsCache.TryGetValue(text, out var val))
                return val;
            return null;
        }

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

                    foreach (var entity in cache.creatureTemplateCache)
                        creatureTemplateByEntry[entity.Entry] = entity;

                    foreach (var entity in cache.gameObjectTemplateCache)
                        gameObjectTemplateByEntry[entity.Entry] = entity;

                    foreach (var entity in cache.questTemplateCache)
                        questTemplateByEntry[entity.Entry] = entity;

                    cache.creatureTemplateByEntry = creatureTemplateByEntry;
                    cache.gameObjectTemplateByEntry = gameObjectTemplateByEntry;
                    cache.questTemplateByEntry = questTemplateByEntry;
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