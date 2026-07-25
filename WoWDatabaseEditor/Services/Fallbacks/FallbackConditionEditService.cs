using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
public class FallbackConditionEditService : IConditionEditService
{
    public Task<IEnumerable<ICondition>?> EditConditions(IDatabaseProvider.ConditionKey conditionKey, IReadOnlyList<ICondition>? conditions, string? customTitle = null)
        => Task.FromResult<IEnumerable<ICondition>?>(null);

    public Task EditConditions(IDatabaseProvider.ConditionKeyMask conditionKeyMask, IDatabaseProvider.ConditionKey conditionKey, string? customTitle = null)
        => Task.CompletedTask;

    public void OpenStandaloneConditions(IDatabaseProvider.ConditionKey conditionKey)
    {
    }
}
