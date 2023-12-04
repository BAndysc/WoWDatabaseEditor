using System;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.Downloaders.MySql;

[AutoRegister]
[SingleInstance]
internal class MySqlDumpDownloadService : IMySqlDumpDownloadService
{
    private readonly IWindowManager windowManager;
    private readonly Func<MySqlDownloadViewModel> creator;

    public MySqlDumpDownloadService(IWindowManager windowManager,
        Func<MySqlDownloadViewModel> creator)
    {
        this.windowManager = windowManager;
        this.creator = creator;
    }
    
    public async Task<string?> AskToDownloadMySqlDumpAsync()
    {
        using var vm = creator();
        if (await windowManager.ShowDialog(vm))
            return vm.DownloadPath;
        return null;
    }
}