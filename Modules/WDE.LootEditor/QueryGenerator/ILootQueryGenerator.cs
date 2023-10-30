using System.Collections.Generic;
using WDE.Common.Database;
using WDE.LootEditor.Models;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.LootEditor.QueryGenerator;

[UniqueProvider]
public interface ILootQueryGenerator
{
    IQuery GenerateUpdateLootIds(LootSourceType sourceType, uint solutionEntry, uint difficultyId, IReadOnlyList<LootEntry> rootLootEntries);
    IQuery GenerateQuery(IReadOnlyList<LootGroupModel> models);
}