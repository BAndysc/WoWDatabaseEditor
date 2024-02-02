using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IConditionEditService
    {
        Task<IEnumerable<ICondition>?> EditConditions(IDatabaseProvider.ConditionKey conditionKey, IReadOnlyList<ICondition>? conditions, string? customTitle = null);

        /// <summary>
        /// Opens the conditions editor for exact condition key and mask 
        /// </summary>
        Task EditConditions(IDatabaseProvider.ConditionKeyMask conditionKeyMask, IDatabaseProvider.ConditionKey conditionKey, string? customTitle = null);

        /// <summary>
        /// Opens the condition editor for the specific key
        /// </summary>
        void OpenStandaloneConditions(IDatabaseProvider.ConditionKey conditionKey);
    }
}