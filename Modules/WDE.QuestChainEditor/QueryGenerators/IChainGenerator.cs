using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.QueryGenerators;

[UniqueProvider]
public interface IChainGenerator
{
    public Task<IEnumerable<ChainRawData>> Generate(QuestStore questStore,
        IReadOnlyDictionary<uint, ChainRawData> existingDataHint);
}