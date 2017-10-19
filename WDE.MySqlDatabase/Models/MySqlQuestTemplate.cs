using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaolinq;
using WDE.Common.Database;

namespace WDE.MySqlDatabase.Models
{
    [DataAccessObject(Name = "quest_template")]
    public abstract class MySqlQuestTemplate : DataAccessObject, IQuestTemplate
    {
        [PrimaryKey]
        [PersistedMember(Name = "entry")]
        public abstract uint Entry { get; set; }

        [PersistedMember(Name = "title")]
        public abstract string Name { get; set; }
    }
}
