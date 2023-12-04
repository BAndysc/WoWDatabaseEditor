using System;
using System.Threading.Tasks;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.Connection;

[AutoRegister]
[SingleInstance]
internal class QuerySafetyService : IQuerySafetyService
{
    private readonly IMessageBoxService messageBoxService;

    public QuerySafetyService(IMessageBoxService messageBoxService)
    {
        this.messageBoxService = messageBoxService;
    }

    private async Task<bool> AskAsync(string query)
    {
        return await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Confirm?")
            .SetMainInstruction("Do you want to execute the query?")
            .SetContent("The following query will be executed:\n\n" + query)
            .WithYesButton(true)
            .WithCancelButton(false)
            .Build());
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