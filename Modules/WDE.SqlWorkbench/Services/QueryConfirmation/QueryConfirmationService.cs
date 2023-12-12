using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Services.QueryConfirmation;

[AutoRegister]
[SingleInstance]
internal class QueryConfirmationService : IQueryConfirmationService
{
    private readonly IWindowManager windowManager;
    private readonly IContainerProvider containerProvider;

    public QueryConfirmationService(IWindowManager windowManager,
        IContainerProvider containerProvider)
    {
        this.windowManager = windowManager;
        this.containerProvider = containerProvider;
    }

    public async Task<QueryConfirmationResult> QueryConfirmationAsync(string query, Func<Task> execute)
    {
        using var vm = containerProvider.Resolve<QueryConfirmationViewModel>((typeof(Func<Task>), execute), (typeof(string), query));
        if (await windowManager.ShowDialog(vm))
            return QueryConfirmationResult.AlreadyExecuted;
        return QueryConfirmationResult.DontExecute;
    }
}