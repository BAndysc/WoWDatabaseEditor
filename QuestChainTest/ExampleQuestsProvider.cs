using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;

namespace QuestChainTest
{
    internal class ExampleQuestsProvider : IQuestsProvider
    {
        public IEnumerable<QuestDefinition> Quests => new List<QuestDefinition>()
        {
                new QuestDefinition(26058, "In Defense of Krom'gar Fortress"),
                new QuestDefinition(26048, "Spare Parts Up In Here!"),
                new QuestDefinition(26047, "And That's Why They Call Them Peons...")
        };
    }

}