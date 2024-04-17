using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Providers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services.RacePickerService;

[SingleInstance]
[AutoRegister]
public class RaceProviderService : IRaceProviderService
{
    private readonly IContainerProvider containerProvider;
    private readonly IWindowManager windowManager;

    public RaceProviderService(IContainerProvider containerProvider,
        IWindowManager windowManager)
    {
        this.containerProvider = containerProvider;
        this.windowManager = windowManager;
    }

    public async Task<CharacterRaces?> PickRaces(CharacterRaces currentRaces)
    {
        using var vm = containerProvider.Resolve<RaceProviderViewModel>(((typeof(CharacterRaces), currentRaces)));
        if (await windowManager.ShowDialog(vm))
        {
            return vm.Value;
        }

        return null;
    }
}