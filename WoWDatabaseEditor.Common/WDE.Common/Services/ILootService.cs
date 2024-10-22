using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ILootService
{
    Task OpenStandaloneLootEditor();
    void OpenStandaloneLootEditor(LootSourceType type, uint solutionEntry, uint difficultyId);
    void OpenStandaloneLootEditor(LootSourceType type, LootEntry rawLootEntry);
    Task EditLoot(LootSourceType type, uint solutionEntry, uint difficultyId);
}