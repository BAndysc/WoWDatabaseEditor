using System;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services.SqlDump;

[AutoRegister]
[SingleInstance]
internal class DatabaseDumpService : IDatabaseDumpService
{
    private readonly IContainerProvider containerProvider;
    private readonly Lazy<IWindowManager> windowManager;

    public DatabaseDumpService(IContainerProvider containerProvider,
        Lazy<IWindowManager> windowManager)
    {
        this.containerProvider = containerProvider;
        this.windowManager = windowManager;
    }

    public async Task ShowDumpDatabaseWindowAsync(DatabaseCredentials credentials)
    {
        await ShowDumpTableWindowAsync(credentials, null);
    }

    public async Task ShowDumpTableWindowAsync(DatabaseCredentials credentials, string? table)
    {
        using var vm = containerProvider.Resolve<DumpViewModel>((typeof(DatabaseCredentials), credentials), (typeof(string), table));
        windowManager.Value.ShowWindow(vm, out var task);
        await task;
    }
}