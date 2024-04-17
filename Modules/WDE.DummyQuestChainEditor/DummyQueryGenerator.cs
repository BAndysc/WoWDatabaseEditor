using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Exceptions;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.QueryGenerators;
using WDE.SqlQueryGenerator;

namespace WDE.DummyQuestChainEditor;

[AutoRegister]
[SingleInstance]
public class DummyQueryGenerator : IQueryGenerator
{
    public async Task<IQuery> GenerateQuery(IList<ChainRawData> data,
        IReadOnlyDictionary<uint, ChainRawData>? existing,
        IReadOnlyDictionary<uint, IReadOnlyList<ICondition>> conditions,
        IReadOnlyDictionary<uint, IReadOnlyList<ICondition>>? oldConditions)
    {
        throw new UserException("Quest chain saving is only available in the pre-built editor due to different licensing of the module.");
    }
}