using System;
using System.Threading.Tasks;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Services.UserQuestions;

namespace WDE.SqlWorkbench.Services.Connection;

[AutoRegister]
[SingleInstance]
internal class QuerySafetyService : IQuerySafetyService
{
    private readonly IUserQuestionsService userQuestionsService;

    public QuerySafetyService(IUserQuestionsService userQuestionsService)
    {
        this.userQuestionsService = userQuestionsService;
    }

    private async Task<bool> AskAsync(string query)
    {
        return await userQuestionsService.ConfirmExecuteQueryAsync(query);
    }

    public async Task<bool> CanExecuteAsync(string query, QueryExecutionSafety safety)
    {
        if (safety == QueryExecutionSafety.ExecuteAll)
            return true;

        if (safety == QueryExecutionSafety.AlwaysAsk)
            return await AskAsync(query);

        if (safety == QueryExecutionSafety.AskUnlessSelect)
        {
            if (query.AsSpan().Trim().StartsWith("select", StringComparison.OrdinalIgnoreCase) ||
                query.AsSpan().Trim().StartsWith("show", StringComparison.OrdinalIgnoreCase))
                return true;
            
            return await AskAsync(query);
        }

        throw new ArgumentOutOfRangeException(nameof(safety), safety + " is not supported");
    }
}