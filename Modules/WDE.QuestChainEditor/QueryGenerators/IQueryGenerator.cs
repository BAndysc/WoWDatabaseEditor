using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.QueryGenerators;

[UniqueProvider]
public interface IQueryGenerator
{
    public IEnumerable<ChainRawData> Generate(QuestStore questStore);
}