using System.Collections.Generic;
using System.Threading.Tasks;

namespace WDE.Conditions.Data
{
    public interface IConditionDataJsonProvider
    {
        Task<string> GetConditionsJson();
        Task<string> GetConditionSourcesJson();
        Task<string> GetConditionGroupsJson();
    }

    public interface IConditionDataProvider
    {
        Task<IReadOnlyList<ConditionJsonData>> GetConditions();
        Task<IReadOnlyList<ConditionSourcesJsonData>> GetConditionSources();
        Task<IReadOnlyList<ConditionGroupsJsonData>> GetConditionGroups();
    }
}