using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Exceptions;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.QueryGenerators;

namespace WDE.DummyQuestChainEditor;

[AutoRegister]
[SingleInstance]
public class DummyChainGenerator : IChainGenerator
{
    public async Task<IEnumerable<ChainRawData>> Generate(QuestStore questStore, IReadOnlyDictionary<uint, ChainRawData> existingDataHint)
    {
        throw new UserException("Quest chain saving is only available in the pre-built editor due to different licensing of the module.");
    }
}