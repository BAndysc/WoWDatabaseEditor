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


        [RelatedDataAccessObjects]
        public abstract RelatedDataAccessObjects<MySqlQuestTemplateAddon> MySqlQuestTemplateAddons { get; }

        public uint PrevQuestId => MySqlQuestTemplateAddons.FirstOrDefault().PrevQuestId;
            
        public uint NextQuestId => MySqlQuestTemplateAddons.FirstOrDefault().NextQuestId;

        public uint ExclusiveGroup => MySqlQuestTemplateAddons.FirstOrDefault().ExclusiveGroup;

        public uint NextQuestInChain => 0;
    }
}
