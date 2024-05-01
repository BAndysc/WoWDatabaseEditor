using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Services;

[UniqueProvider]
public interface IQuestChainLoader
{
    Task LoadChain(uint[] quests, QuestStore store, List<string>? nonFatalErrors = null, CancellationToken token = default);
}