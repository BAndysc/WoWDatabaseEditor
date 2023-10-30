using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.LootEditor.Services;

public interface ILootCrossReferencesService
{
    Task OpenCrossReferencesDialog(LootSourceType type, LootEntry lootId);
}