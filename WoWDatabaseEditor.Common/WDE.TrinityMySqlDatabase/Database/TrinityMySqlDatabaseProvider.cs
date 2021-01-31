using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.TrinityMySqlDatabase.Data;
using WDE.TrinityMySqlDatabase.Models;
using WDE.TrinityMySqlDatabase.Providers;
using WDE.TrinityMySqlDatabase.Services;

namespace WDE.TrinityMySqlDatabase.Database
{
    public class TrinityMySqlDatabaseProvider : IDatabaseProvider
    {
        public TrinityMySqlDatabaseProvider(IConnectionSettingsProvider settings,
            DatabaseLogger databaseLogger,
            ITaskRunner taskRunner)
        {
            string? host = settings.GetSettings().Host;
            DataConnection.TurnTraceSwitchOn();
            DataConnection.WriteTraceLine = databaseLogger.Log;
            DataConnection.DefaultSettings = new MySqlSettings(settings.GetSettings());
        }

        public ICreatureTemplate? GetCreatureTemplate(uint entry)
        {
            using var model = new TrinityDatabase();
            return model.CreatureTemplate.FirstOrDefault(ct => ct.Entry == entry);
        }

        public IEnumerable<ICreatureTemplate> GetCreatureTemplates()
        {
            var task = GetCreatureTemplatesAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task<List<MySqlCreatureTemplate>> GetCreatureTemplatesAsync()
        {
            await using var model = new TrinityDatabase();
            return await model.CreatureTemplate.OrderBy(t => t.Entry).ToListAsync();
        }

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            using var model = new TrinityDatabase();
            return model.SmartScript.Where(line => line.EntryOrGuid == entryOrGuid && line.ScriptSourceType == (int) type).ToList();
        }

        public IEnumerable<IGameEvent> GetGameEvents()
        {
            var task = GetGameEventsAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task<List<MySqlGameEvent>> GetGameEventsAsync()
        {
            using var model = new TrinityDatabase();
            return await (from t in model.GameEvents orderby t.Entry select t).ToListAsync();
        }
        
        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates()
        {
            var task = GetGameObjectTemplatesAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task<List<MySqlGameObjectTemplate>> GetGameObjectTemplatesAsync()
        {
            using var model = new TrinityDatabase();
            return await (from t in model.GameObjectTemplate orderby t.Entry select t).ToListAsync();
        }

        public IEnumerable<IQuestTemplate> GetQuestTemplates()
        {
            var task = GetQuestTemplatesAsync();
            task.Wait();
            return task.Result;
        }
        
        public async Task<List<MySqlQuestTemplate>> GetQuestTemplatesAsync()
        {
            using var model = new TrinityDatabase();

            return await (from t in model.QuestTemplate
                    join addon in model.QuestTemplateAddon on t.Entry equals addon.Entry into adn
                    from subaddon in adn.DefaultIfEmpty()
                    orderby t.Entry
                    select t.SetAddon(subaddon)).ToListAsync();
        }

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
            using var model = new TrinityDatabase();
            return model.GameObjectTemplate.FirstOrDefault(g => g.Entry == entry);
        }

        public IQuestTemplate? GetQuestTemplate(uint entry)
        {
            using var model = new TrinityDatabase();
            MySqlQuestTemplateAddon? addon = model.QuestTemplateAddon.FirstOrDefault(addon => addon.Entry == entry);
            return model.QuestTemplate.FirstOrDefault(q => q.Entry == entry)?.SetAddon(addon);
        }

        public async Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script)
        {
            using var writeLock = await MySqlSingleWriteLock.WriteLock();
            await using var model = new TrinityDatabase();

            await model.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            await model.SmartScript.Where(x => x.EntryOrGuid == entryOrGuid && x.ScriptSourceType == (int) type).DeleteAsync();
            if (type == SmartScriptType.Creature)
            {
                await model.CreatureTemplate.Where(p => p.Entry == (uint) entryOrGuid)
                    .Set(p => p.AIName, "SmartAI")
                    .Set(p => p.ScriptName, "")
                    .UpdateAsync();
            }
            
            await model.SmartScript.BulkCopyAsync(script.Select(l => new MySqlSmartScriptLine(l)));

            await model.CommitTransactionAsync();
        }

        public async Task InstallConditions(IEnumerable<IConditionLine> conditionLines,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null)
        {
            using var writeLock = await MySqlSingleWriteLock.WriteLock();
            await using var model = new TrinityDatabase();

            var conditions = conditionLines?.ToList() ?? new List<IConditionLine>();
            List<(int SourceType, int? SourceGroup, int? SourceEntry, int? SourceId)> keys = conditions.Select(c =>
                    (c.SourceType, keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceGroup) ? (int?) c.SourceGroup : null,
                        keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceEntry) ? (int?) c.SourceEntry : null,
                        keyMask.HasFlag(IDatabaseProvider.ConditionKeyMask.SourceId) ? (int?) c.SourceId : null))
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
            using var model = new TrinityDatabase();

            return model.Conditions.Where(line =>
                    line.SourceType == sourceType && line.SourceEntry == sourceEntry && line.SourceId == sourceId)
                .ToList();
        }
    }

    public class ConnectionStringSettings : IConnectionStringSettings
    {
        public string ConnectionString { get; set; } = "";
        public string Name { get; set; } = "";
        public string ProviderName { get; set; } = "";
        public bool IsGlobal => false;
    }

    public class MySqlSettings : ILinqToDBSettings
    {
        public MySqlSettings(DbAccess access)
        {
            ConnectionStrings = new[]
            {
                new ConnectionStringSettings
                {
                    Name = "Trinity",
                    ProviderName = "MySqlConnector",
                    ConnectionString =
                        $"Server={access.Host};Port={access.Port ?? 3306};Database={access.Database};Uid={access.User};Pwd={access.Password};AllowUserVariables=True"
                }
            };
        }

        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "MySqlConnector";
        public string DefaultDataProvider => "MySqlConnector";

        public IEnumerable<IConnectionStringSettings> ConnectionStrings { get; }
    }
}