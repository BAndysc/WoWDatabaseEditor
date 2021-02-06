using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.TrinityMySqlDatabase.Models;

namespace WDE.TrinityMySqlDatabase.Database
{
    public class CachedDatabaseProvider : IDatabaseProvider
    {
        private List<MySqlCreatureTemplate>? creatureTemplateCache;
        private Dictionary<uint, MySqlCreatureTemplate> creatureTemplateByEntry = new();

        private List<MySqlGameObjectTemplate>? gameObjectTemplateCache;
        private Dictionary<uint, MySqlGameObjectTemplate> gameObjectTemplateByEntry = new();

        private List<MySqlQuestTemplate>? questTemplateCache;
        private Dictionary<uint, MySqlQuestTemplate> questTemplateByEntry = new();

        private List<MySqlAreaTriggerTemplate>? areaTriggerTemplates;
        private List<MySqlGameEvent>? gameEventsCache;
        
        private TrinityMySqlDatabaseProvider trinityDatabase;
        private readonly ITaskRunner taskRunner;

        public CachedDatabaseProvider(TrinityMySqlDatabaseProvider trinityDatabase, ITaskRunner taskRunner)
        {
            this.trinityDatabase = trinityDatabase;
            this.taskRunner = taskRunner;
        }

        public void TryConnect()
        {
            trinityDatabase.GetCreatureTemplate(0);
            taskRunner.ScheduleTask(new DatabaseCacheTask(this));;
        }

        public ICreatureTemplate? GetCreatureTemplate(uint entry)
        {
            if (creatureTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return trinityDatabase.GetCreatureTemplate(entry);
        }

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
            if (gameObjectTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return trinityDatabase.GetGameObjectTemplate(entry);
        }

        public IQuestTemplate? GetQuestTemplate(uint entry)
        {
            if (questTemplateByEntry.TryGetValue(entry, out var template))
                return template;

            return trinityDatabase.GetQuestTemplate(entry);
        }

        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates()
        {
            if (gameObjectTemplateCache != null)
                return gameObjectTemplateCache;

            return trinityDatabase.GetGameObjectTemplates();
        }

        public IEnumerable<ICreatureTemplate> GetCreatureTemplates()
        {
            if (creatureTemplateCache != null)
                return creatureTemplateCache;

            return trinityDatabase.GetCreatureTemplates();
        }

        public IEnumerable<IQuestTemplate> GetQuestTemplates()
        {
            if (questTemplateCache != null)
                return questTemplateCache;

            return trinityDatabase.GetQuestTemplates();
        }

        public IEnumerable<IGameEvent> GetGameEvents() => gameEventsCache ?? trinityDatabase.GetGameEvents();

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates() =>
            areaTriggerTemplates ?? trinityDatabase.GetAreaTriggerTemplates();

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            return trinityDatabase.GetScriptFor(entryOrGuid, type);
        }

        public async Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script)
        {
            await trinityDatabase.InstallScriptFor(entryOrGuid, type, script);
        }

        public async Task InstallConditions(IEnumerable<IConditionLine> conditions,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null)
        {
            await trinityDatabase.InstallConditions(conditions, keyMask, manualKey);
        }

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId)
        {
            return trinityDatabase.GetConditionsFor(sourceType, sourceEntry, sourceId);
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
                int steps = 5;
                
                progress.Report(0, steps, "Loading creatures");
                cache.creatureTemplateCache = await cache.trinityDatabase.GetCreatureTemplatesAsync();

                progress.Report(1, steps, "Loading gameobjects");
                cache.gameObjectTemplateCache = await cache.trinityDatabase.GetGameObjectTemplatesAsync();

                progress.Report(2, steps, "Loading game events");
                cache.gameEventsCache = await cache.trinityDatabase.GetGameEventsAsync();
                
                progress.Report(3, steps, "Loading areatrigger templates");
                cache.areaTriggerTemplates = await cache.trinityDatabase.GetAreaTriggerTemplatesAsync();
                
                progress.Report(4, steps, "Loading quests");

                cache.questTemplateCache = await cache.trinityDatabase.GetQuestTemplatesAsync();

                Dictionary<uint, MySqlCreatureTemplate> creatureTemplateByEntry = new();
                Dictionary<uint, MySqlGameObjectTemplate> gameObjectTemplateByEntry = new();
                Dictionary<uint, MySqlQuestTemplate> questTemplateByEntry = new();

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