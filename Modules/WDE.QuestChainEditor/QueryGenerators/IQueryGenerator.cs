using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QuestChainEditor.QueryGenerators;

[UniqueProvider]
public interface IQueryGenerator
{
    Task<IQuery> GenerateQuery(IList<ChainRawData> data,
        IReadOnlyDictionary<uint, ChainRawData>? existing,
        IReadOnlyDictionary<uint, IReadOnlyList<ICondition>> conditions,
        IReadOnlyDictionary<uint, IReadOnlyList<ICondition>>? oldConditions);
}