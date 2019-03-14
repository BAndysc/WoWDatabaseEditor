using Shaolinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.TrinityMySqlDatabase.Models
{
    [DataAccessObject(Name = "quest_template_addon")]
    public abstract class MySqlQuestTemplateAddon : DataAccessObject
    {
        [PrimaryKey]
        [PersistedMember(Name = "ID")]
        public abstract uint QuestId { get; set; }

        [PersistedMember(Name = "PrevQuestId")]
        public abstract int PrevQuestId { get; set; }

        [PersistedMember(Name = "NextQuestId")]
        public abstract int NextQuestId { get; set; }

        [PersistedMember(Name = "ExclusiveGroup")]
        public abstract int ExclusiveGroup { get; set; }
    }
}
