using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ILootService
{
    Task OpenStandaloneLootEditor();
    Task EditLoot(LootSourceType type, uint solutionEntry, uint difficultyId);
}