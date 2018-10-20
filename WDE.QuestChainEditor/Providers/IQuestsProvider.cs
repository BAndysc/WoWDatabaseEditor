using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Providers
{
    public interface IQuestsProvider
    {
        IEnumerable<QuestDefinition> Quests { get; }
    }
}
