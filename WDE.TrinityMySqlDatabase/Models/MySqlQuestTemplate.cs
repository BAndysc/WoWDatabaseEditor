using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaolinq;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [DataAccessObject(Name = "quest_template")]
    public abstract class MySqlQuestTemplate : DataAccessObject, IQuestTemplate
    {
        [PrimaryKey]
        [PersistedMember(Name = "ID")]
        public abstract uint Entry { get; set; }

        [PersistedMember(Name = "LogTitle")]
        public abstract string Name { get; set; }

        public int PrevQuestId => addon == null ? 0 : addon.PrevQuestId;

        public int NextQuestId => addon == null ? 0 : addon.NextQuestId;

        public int ExclusiveGroup => addon == null ? 0 : addon.ExclusiveGroup;

        public int NextQuestInChain => 0;
        

        private MySqlQuestTemplateAddon addon;

        public MySqlQuestTemplate SetAddon(MySqlQuestTemplateAddon addon)
        {
            this.addon = addon;
            return this;
        }
    }
}
