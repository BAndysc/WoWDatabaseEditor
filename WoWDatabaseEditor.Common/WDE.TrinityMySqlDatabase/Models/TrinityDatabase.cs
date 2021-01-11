using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;

namespace WDE.TrinityMySqlDatabase.Models
{
    public class TrinityDatabase : LinqToDB.Data.DataConnection
    {
        public TrinityDatabase() : base("Trinity") { }
        
        public ITable<MySqlCreatureTemplate> CreatureTemplate => GetTable<MySqlCreatureTemplate>();
        public ITable<MySqlSmartScriptLine> SmartScript => GetTable<MySqlSmartScriptLine>();
        public ITable<MySqlGameObjectTemplate> GameObjectTemplate => GetTable<MySqlGameObjectTemplate>();
        public ITable<MySqlQuestTemplate> QuestTemplate => GetTable<MySqlQuestTemplate>();
        public ITable<MySqlQuestTemplateAddon> QuestTemplateAddon => GetTable<MySqlQuestTemplateAddon>();
        public ITable<MySqlConditionLine> Conditions => GetTable<MySqlConditionLine>();
    }
}
