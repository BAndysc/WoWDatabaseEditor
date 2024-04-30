using System;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services.SqlImport;

[AutoRegister]
[SingleInstance]
internal class DatabaseImportService : IDatabaseImportService
{
    private readonly IContainerProvider containerProvider;
    private readonly Lazy<IWindowManager> windowManager;

    public DatabaseImportService(IContainerProvider containerProvider,
        Lazy<IWindowManager> windowManager)
    {
        this.containerProvider = containerProvider;
        this.windowManager = windowManager;
    }

    public async Task ShowImportDatabaseWindowAsync(DatabaseCredentials credentials)
    {

        using var vm = containerProvider.Resolve<ImportViewModel>((typeof(DatabaseCredentials), credentials));
        windowManager.Value.ShowWindow(vm, out var task);
        await task;
    }
}