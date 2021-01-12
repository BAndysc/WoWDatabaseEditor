using System.Collections.Generic;
using System.Linq;
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

        public IDatabaseProvider Database { get; }

        public IEnumerable<QuestDefinition> Quests => Database.GetQuestTemplates().Select(t => new QuestDefinition(t.Entry, t.Name));
    }
}