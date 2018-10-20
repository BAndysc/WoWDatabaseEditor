using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Providers
{
    public class DatabaseQuestsProvider : IQuestsProvider
    {
        public DatabaseQuestsProvider(IDatabaseProvider database)
        {
            Database = database;
        }

        public IEnumerable<QuestDefinition> Quests => Database.GetQuestTemplates().Select(t => new QuestDefinition(t.Entry, t.Name));

        public IDatabaseProvider Database { get; }
    }
}
