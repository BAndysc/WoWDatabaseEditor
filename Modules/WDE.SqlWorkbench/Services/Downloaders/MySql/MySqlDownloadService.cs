using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.Downloaders.MySql;

[AutoRegister]
[SingleInstance]
internal class MySqlDownloadService : IMySqlDownloadService
{
    private readonly IWindowManager windowManager;
    private readonly Func<MySqlDownloadViewModel> creator;

    public MySqlDownloadService(IWindowManager windowManager,
        Func<MySqlDownloadViewModel> creator)
    {
        this.windowManager = windowManager;
        this.creator = creator;
    }
    
    public async Task<(string dump, string mysql)?> AskToDownloadMySqlAsync()
    {
        using var vm = creator();
        if (await windowManager.ShowDialog(vm))
            return vm.DumpDownloadPath != null &&
                   vm.MySqlDownloadPath != null ?
                (vm.DumpDownloadPath, vm.MySqlDownloadPath) : null;
        return null;
    }
}