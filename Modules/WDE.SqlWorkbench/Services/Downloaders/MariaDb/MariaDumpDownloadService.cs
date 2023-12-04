using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.Downloaders.MariaDb;

[AutoRegister]
[SingleInstance]
internal class MariaDumpDownloadService : IMariaDumpDownloadService
{
    private readonly IWindowManager windowManager;
    private readonly Func<MariaDownloadViewModel> creator;

    public MariaDumpDownloadService(IWindowManager windowManager,
        Func<MariaDownloadViewModel> creator)
    {
        this.windowManager = windowManager;
        this.creator = creator;
    }
    
    public async Task<string?> AskToDownloadMariaDumpAsync()
    {
        using var vm = creator();
        if (await windowManager.ShowDialog(vm))
            return vm.DownloadPath;
        return null;
    }
}