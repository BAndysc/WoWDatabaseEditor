using System.Threading.Tasks;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Services.QueryConfirmation;

internal interface IQueryConfirmationService
{
    Task<bool> QueryConfirmationAsync(IConnection connection, string query);
}