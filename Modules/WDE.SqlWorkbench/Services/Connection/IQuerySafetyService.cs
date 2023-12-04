using System.Threading.Tasks;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IQuerySafetyService
{
    Task<bool> CanExecuteAsync(string query, QueryExecutionSafety safety);
}