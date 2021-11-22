using System.Collections.Generic;
using System.Threading.Tasks;

namespace WDE.Conditions.Data
{
    public interface IConditionDataJsonProvider
    {
        string GetConditionsJson();
        string GetConditionSourcesJson();
        string GetConditionGroupsJson();
    }

    public interface IConditionDataProvider
    {
        IEnumerable<ConditionJsonData> GetConditions();
        IEnumerable<ConditionSourcesJsonData> GetConditionSources();
        IEnumerable<ConditionGroupsJsonData> GetConditionGroups();
    }
}