using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using MySql.Data.MySqlClient;
using LinqToDB.Mapping;
using WDE.Module.Attributes;
using WDE.Common.Database;
using WDE.TrinityMySqlDatabase.Data;
using WDE.TrinityMySqlDatabase.Models;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister, SingleInstance]
    public class TrinityMysqlDatabaseProvider : IDatabaseProvider
    {
        private TrinityDatabase? model;

        private List<MySqlCreatureTemplate>? CreatureTemplateCache;

        private List<MySqlGameObjectTemplate>? GameObjectTemplateCache;

        private List<MySqlQuestTemplate>? QuestTemplateCache;

        public TrinityMysqlDatabaseProvider(IConnectionSettingsProvider settings)
        {
            string? Host = settings.GetSettings().Host;
            try
            {
                DataConnection.DefaultSettings = new MySqlSettings(settings.GetSettings());
                model = new TrinityDatabase();
                var temp = GetCreatureTemplates().ToList();
            }
            catch (Exception e)
            {
                if (!string.IsNullOrEmpty(Host))
                    MessageBox.Show($"Cannot connect to MySql database: {e.Message} Check your settings.");
                model = null;
            }
        }

        public ICreatureTemplate? GetCreatureTemplate(uint entry)
        {
            if (model == null)
                return null;

            if (model.CreatureTemplate.Count(x => x.Entry == entry) == 0)
                return null;

            return model.CreatureTemplate.FirstOrDefault(ct => ct.Entry == entry);
        }

        public IEnumerable<ICreatureTemplate> GetCreatureTemplates()
        {
            if (model == null)
                return new List<ICreatureTemplate>();
            if (CreatureTemplateCache == null)
                CreatureTemplateCache = (from t in model.CreatureTemplate orderby t.Entry select t).ToList();
            return CreatureTemplateCache;
        }

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            if (model == null)
                return new List<ISmartScriptLine>();
            return model.SmartScript.Where((line) => line.EntryOrGuid == entryOrGuid && line.ScriptSourceType == (int)type).ToList();
        }

        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates()
        {
            if (model == null)
                return new List<IGameObjectTemplate>();
            if (GameObjectTemplateCache == null)
                GameObjectTemplateCache = (from t in model.GameObjectTemplate orderby t.Entry select t).ToList();
            return GameObjectTemplateCache;
        }

        public IEnumerable<IQuestTemplate> GetQuestTemplates()
        {
            if (model == null)
                return new List<IQuestTemplate>();
            if (QuestTemplateCache == null)
                QuestTemplateCache = (from t in model.QuestTemplate join addon in model.QuestTemplateAddon on t.Entry equals addon.Entry into adn from subaddon in adn.DefaultIfEmpty() orderby t.Entry select t.SetAddon(subaddon)).ToList();
            return QuestTemplateCache;
        }

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry)
        {
            if (model == null)
                return null;
            if (model.GameObjectTemplate.Count(x => x.Entry == entry) == 0)
                return null;
            return model.GameObjectTemplate.FirstOrDefault(g => g.Entry == entry);
        }

        public IQuestTemplate? GetQuestTemplate(uint entry)
        {
            if (model == null)
                return null;
            if (model.QuestTemplate.Count(x => x.Entry == entry) == 0)
                return null;
            var addon = model.QuestTemplateAddon.FirstOrDefault(addon => addon.Entry == entry);
            return model.QuestTemplate.FirstOrDefault(q => q.Entry == entry)?.SetAddon(addon);
        }

        public void InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script)
        {
            if (model == null)
                return;

            model.BeginTransaction();
            model.SmartScript.Where(x => x.EntryOrGuid == entryOrGuid && x.ScriptSourceType == (int) type).Delete();
            if (type == SmartScriptType.Creature)
            {
                model.CreatureTemplate
                    .Where(p => p.Entry == entryOrGuid)
                    .Set(p => p.AIName, "SmartAI")
                    .Set(p => p.ScriptName, "")
                    .Update();
            }

            foreach (var line in script)
            {
                MySqlSmartScriptLine sqlLine = new ()
                {
                    EntryOrGuid = line.EntryOrGuid,
                    ScriptSourceType = line.ScriptSourceType,
                    Id = line.Id,
                    Link = line.Link,
                    EventType = line.EventType,
                    EventPhaseMask = line.EventPhaseMask,
                    EventChance = line.EventChance,
                    EventFlags = line.EventFlags,
                    EventParam1 = line.EventParam1,
                    EventParam2 = line.EventParam2,
                    EventParam3 = line.EventParam3,
                    EventParam4 = line.EventParam4,
                    EventCooldownMin = line.EventCooldownMin,
                    EventCooldownMax = line.EventCooldownMax,
                    ActionType = line.ActionType,
                    ActionParam1 = line.ActionParam1,
                    ActionParam2 = line.ActionParam2,
                    ActionParam3 = line.ActionParam3,
                    ActionParam4 = line.ActionParam4,
                    ActionParam5 = line.ActionParam5,
                    ActionParam6 = line.ActionParam6,
                    TargetType = line.TargetType,
                    TargetParam1 = line.TargetParam1,
                    TargetParam2 = line.TargetParam2,
                    TargetParam3 = line.TargetParam3,
                    TargetX = line.TargetX,
                    TargetY = line.TargetY,
                    TargetZ = line.TargetZ,
                    TargetO = line.TargetO,
                    Comment = line.Comment
                };
                model.Insert(sqlLine);
            }
            model.CommitTransaction();
        }

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId)
        {
            if (model == null)
                return new List<IConditionLine>();

            return model.Conditions.Where((line) => line.SourceType == sourceType && line.SourceEntry == sourceEntry && line.SourceId == sourceId);
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
        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "MySql";
        public string DefaultDataProvider => "MySql";

        public MySqlSettings(DbAccess access)
        {
            ConnectionStrings = new[]
            {
                new ConnectionStringSettings
                {
                    Name = "Trinity",
                    ProviderName = "MySql",
                    ConnectionString =
                        $"Server={access.Host};Port={access.Port ?? 3306};Database={access.Database};Uid={access.User};Pwd={access.Password};"
                }
            };
        }
        
        public IEnumerable<IConnectionStringSettings> ConnectionStrings { get; }
    }
}
