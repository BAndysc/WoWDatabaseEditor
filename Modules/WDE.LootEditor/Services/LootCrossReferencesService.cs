using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.LootEditor.CrossReferences;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Services;

[AutoRegister]
[SingleInstance]
public class LootCrossReferencesService : ILootCrossReferencesService
{
    private readonly IContainerProvider containerProvider;
    private readonly IWindowManager windowManager;

    public LootCrossReferencesService(IContainerProvider containerProvider,
        IWindowManager windowManager)
    {
        this.containerProvider = containerProvider;
        this.windowManager = windowManager;
    }
    
    public async Task OpenCrossReferencesDialog(LootSourceType type, LootEntry lootId)
    {
        using var vm = containerProvider.Resolve<LootCrossReferencesViewModel>((typeof(LootSourceType), type), (typeof(LootEntry), lootId));
        await windowManager.ShowDialog(vm);
    }
}