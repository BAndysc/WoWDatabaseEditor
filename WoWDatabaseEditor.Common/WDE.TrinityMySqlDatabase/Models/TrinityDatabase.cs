using LinqToDB;
using LinqToDB.Data;

namespace WDE.TrinityMySqlDatabase.Models
{
    public class TrinityDatabase : DataConnection
    {
        public TrinityDatabase() : base("Trinity")
        {
        }

        public ITable<MySqlCreatureTemplate> CreatureTemplate => GetTable<MySqlCreatureTemplate>();
        public ITable<MySqlSmartScriptLine> SmartScript => GetTable<MySqlSmartScriptLine>();
        public ITable<MySqlGameObjectTemplate> GameObjectTemplate => GetTable<MySqlGameObjectTemplate>();
        public ITable<MySqlQuestTemplate> QuestTemplate => GetTable<MySqlQuestTemplate>();
        public ITable<MySqlQuestTemplateAddon> QuestTemplateAddon => GetTable<MySqlQuestTemplateAddon>();
        public ITable<MySqlGameEvent> GameEvents => GetTable<MySqlGameEvent>();
        public ITable<MySqlConditionLine> Conditions => GetTable<MySqlConditionLine>();
    }
}