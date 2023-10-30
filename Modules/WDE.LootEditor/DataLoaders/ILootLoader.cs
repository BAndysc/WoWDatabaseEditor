using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.LootEditor.Models;
using WDE.Module.Attributes;

namespace WDE.LootEditor.DataLoaders;

[UniqueProvider]
public interface ILootLoader
{
    // i.e. gameobject_loot is linked to gameobject_template via data1 column
    Task<IReadOnlyList<LootEntry>> GetLootEntries(LootSourceType type, uint solutionItemEntry, uint difficulty);
    
    Task<IReadOnlyList<LootModel>> FetchLoot(LootSourceType type, LootEntry entry);
    Task<ILootTemplateName?> GetLootTemplateName(LootSourceType type, LootEntry entry);
}