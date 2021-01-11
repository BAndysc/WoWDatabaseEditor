using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "quest_template")]
    public class MySqlQuestTemplate : IQuestTemplate
    {
        [PrimaryKey]
        [Column(Name = "ID")]
        public uint Entry { get; set; }

        [Column(Name = "LogTitle")] 
        public string Name { get; set; } = "";

        public int PrevQuestId => addon == null ? 0 : addon.PrevQuestId;

        public int NextQuestId => addon == null ? 0 : addon.NextQuestId;

        public int ExclusiveGroup => addon == null ? 0 : addon.ExclusiveGroup;

        public int NextQuestInChain => 0;
        

        private MySqlQuestTemplateAddon? addon;

        public MySqlQuestTemplate SetAddon(MySqlQuestTemplateAddon? addon)
        {
            this.addon = addon;
            return this;
        }
    }
}
