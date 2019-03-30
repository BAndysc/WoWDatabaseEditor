using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using MySql.Data.MySqlClient;
using Shaolinq;
using WDE.Module.Attributes;
using WDE.Common.Database;
using WDE.TrinityMySqlDatabase.Models;
using WDE.TrinityMySqlDatabase.Providers;
using MySqlConfiguration = Shaolinq.MySql.MySqlConfiguration;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister, SingleInstance]
    public class TrinityMysqlDatabaseProvider : IDatabaseProvider
    {
        private TrinityDatabase model;

        private List<MySqlCreatureTemplate> CreatureTemplateCache;

        private List<MySqlGameObjectTemplate> GameObjectTemplateCache;

        private List<MySqlQuestTemplate> QuestTemplateCache;

        public TrinityMysqlDatabaseProvider(IConnectionSettingsProvider settings)
        {
            string Database = settings.GetSettings().DB;
            string User = settings.GetSettings().User;
            string Password = settings.GetSettings().Password;
            string Host = settings.GetSettings().Host;
            try
            {
                var config = MySqlConfiguration.Create(Database, Host, User, Password);
                model = DataAccessModel.BuildDataAccessModel<TrinityDatabase>(config);
                try
                {
                    model.Create(DatabaseCreationOptions.IfDatabaseNotExist);
                }
                catch (Exception)
                {
                    // already exists, its ok
                }
                var temp = GetCreatureTemplates();
            }
            catch (Exception e)
            {
                if (!string.IsNullOrEmpty(Host))
                    MessageBox.Show($"Cannot connect to MySql database: {e.Message} Check your settings.");
                model = null;
            }
        }

        public ICreatureTemplate GetCreatureTemplate(uint entry)
        {
            if (model == null)
                return null;

            if (model.CreatureTemplate.Count(x => x.Entry == entry) == 0)
                return null;

            return model.CreatureTemplate.GetReference(entry);
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
                QuestTemplateCache = (from t in model.QuestTemplate join addon in model.QuestTemplateAddon on t.Entry equals addon.QuestId into adn from subaddon in adn.DefaultIfEmpty() orderby t.Entry select t.SetAddon(subaddon)).ToList();
            return QuestTemplateCache;
        }

        public IGameObjectTemplate GetGameObjectTemplate(uint entry)
        {
            if (model == null)
                return null;
            if (model.GameObjectTemplate.Count(x => x.Entry == entry) == 0)
                return null;
            return model.GameObjectTemplate.GetReference(entry);
        }

        public IQuestTemplate GetQuestTemplate(uint entry)
        {
            if (model == null)
                return null;
            if (model.QuestTemplate.Count(x => x.Entry == entry) == 0)
                return null;
            var addon = model.QuestTemplateAddon.GetReference(entry);
            return model.QuestTemplate.GetReference(entry).SetAddon(addon);
        }

        public void InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script)
        {
            if (model == null)
                return;

            using (var scope = new TransactionScope())
            {
                model.SmartScript.Where(x => x.EntryOrGuid == entryOrGuid && x.ScriptSourceType == (int) type).Delete();

                scope.Flush();

                foreach (var line in script)
                {
                    var sqlLine = model.SmartScript.Create(new {EntryOrGuid=line.EntryOrGuid, ScriptSourceType=line.ScriptSourceType, Id=line.Id});
                    sqlLine.Link = line.Link;
                    sqlLine.EventType = line.EventType;
                    sqlLine.EventPhaseMask = line.EventPhaseMask;
                    sqlLine.EventChance = line.EventChance;
                    sqlLine.EventFlags = line.EventFlags;
                    sqlLine.EventParam1 = line.EventParam1;
                    sqlLine.EventParam2 = line.EventParam2;
                    sqlLine.EventParam3 = line.EventParam3;
                    sqlLine.EventParam4 = line.EventParam4;
                    sqlLine.EventCooldownMin = line.EventCooldownMin;
                    sqlLine.EventCooldownMax = line.EventCooldownMax;
                    sqlLine.ActionType = line.ActionType;
                    sqlLine.ActionParam1 = line.ActionParam1;
                    sqlLine.ActionParam2 = line.ActionParam2;
                    sqlLine.ActionParam3 = line.ActionParam3;
                    sqlLine.ActionParam4 = line.ActionParam4;
                    sqlLine.ActionParam5 = line.ActionParam5;
                    sqlLine.ActionParam6 = line.ActionParam6;
                    sqlLine.TargetType = line.TargetType;
                    sqlLine.TargetParam1 = line.TargetParam1;
                    sqlLine.TargetParam2 = line.TargetParam2;
                    sqlLine.TargetParam3 = line.TargetParam3;
                    sqlLine.TargetX = line.TargetX;
                    sqlLine.TargetY = line.TargetY;
                    sqlLine.TargetZ = line.TargetZ;
                    sqlLine.TargetO = line.TargetO;
                    sqlLine.Comment = line.Comment;

                
                }
                scope.Complete();
            }
        }

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId)
        {
            if (model == null)
                return new List<IConditionLine>();

            return model.Conditions.Where((line) => line.SourceType == sourceType && line.SourceEntry == sourceEntry && line.SourceId == sourceId);
        }
    }
}
