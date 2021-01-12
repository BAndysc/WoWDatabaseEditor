using System.Collections.Generic;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Providers
{
    public interface IQuestsProvider
    {
        IEnumerable<QuestDefinition> Quests { get; }
    }
}