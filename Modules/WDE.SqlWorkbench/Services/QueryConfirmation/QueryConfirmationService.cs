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

    public async Task<bool> QueryConfirmationAsync(IConnection connection, string query)
    {
        using var vm = containerProvider.Resolve<QueryConfirmationViewModel>((typeof(IConnection), connection), (typeof(string), query));
        return await windowManager.ShowDialog(vm);
    }
}