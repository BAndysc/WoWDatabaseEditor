using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IConditionEditService
    {
        Task<IEnumerable<ICondition>?> EditConditions(int conditionSourceType, IReadOnlyList<ICondition>? conditions, string? customTitle = null);
        Task EditConditions(IDatabaseProvider.ConditionKeyMask conditionKeyMask, IDatabaseProvider.ConditionKey conditionKey, string? customTitle = null);
    }
}