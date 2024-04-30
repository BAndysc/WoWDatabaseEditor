using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.Downloaders.MariaDb;

[AutoRegister]
[SingleInstance]
internal class MariaDownloadService : IMariaDownloadService
{
    private readonly IWindowManager windowManager;
    private readonly Func<MariaDownloadViewModel> creator;

    public MariaDownloadService(IWindowManager windowManager,
        Func<MariaDownloadViewModel> creator)
    {
        this.windowManager = windowManager;
        this.creator = creator;
    }
    
    public async Task<(string dump, string mariadb)?> AskToDownloadMariaAsync()
    {
        using var vm = creator();
        if (await windowManager.ShowDialog(vm))
            return vm.DumpDownloadPath != null &&
                   vm.MariaDbDownloadPath != null ?
                (vm.DumpDownloadPath, vm.MariaDbDownloadPath) : null;
        return null;
    }
}