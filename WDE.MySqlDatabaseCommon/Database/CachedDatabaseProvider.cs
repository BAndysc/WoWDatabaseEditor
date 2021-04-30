using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Tasks;

namespace WDE.MySqlDatabaseCommon.Database
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
        
        private IAsyncDatabaseProvider nonCachedDatabase;
        private readonly ITaskRunner taskRunner;

        public CachedDatabaseProvider(IAsyncDatabaseProvider nonCachedDatabase, ITaskRunner taskRunner)
        {
            this.nonCachedDatabase = nonCachedDatabase;
            this.taskRunner = taskRunner;
        }

        public void TryConnect()
        {
            nonCachedDatabase.GetCreatureTemplate(0);
            taskRunner.ScheduleTask(new DatabaseCacheTask(this));;
        }

        public bool IsConnected => nonCachedDatabase.IsConnected;

        public ICreatureTemplate? GetCreatureTemplate(uint entry)
        {
            if (creatureTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return nonCachedDatabase.GetCreatureTemplate(entry);
        }

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
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

        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId)
        {
            return nonCachedDatabase.GetSpellScriptNames(spellId);
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
                int steps = 8;
                
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
        }
    }
}