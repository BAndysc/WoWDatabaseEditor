using System;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Services.QueryConfirmation;

internal interface IQueryConfirmationService
{
    Task<QueryConfirmationResult> QueryConfirmationAsync(string query, Func<Task> execute);
}

internal static class QueryConfirmationServiceExtensions
{
    public static Task<QueryConfirmationResult> QueryConfirmationAndExecuteAsync(
        this IQueryConfirmationService service,
        IConnection connection, string query)
    {
        return service.QueryConfirmationAsync(query, async () =>
        {
            await using var session = await connection.OpenSessionAsync();
            await session.ExecuteSqlAsync(query);
        });
    }
}

internal enum QueryConfirmationResult
{
    DontExecute,
    AlreadyExecuted
}