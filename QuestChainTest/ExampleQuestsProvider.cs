using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Providers;

namespace QuestChainTest
{
    internal class ExampleQuestsProvider : IQuestsProvider
    {
        public IEnumerable<QuestDefinition> Quests =>
            new List<QuestDefinition>
            {
                new(26058, "In Defense of Krom'gar Fortress"),
                new(26048, "Spare Parts Up In Here!"),
                new(26047, "And That's Why They Call Them Peons...")
            };
    }
}