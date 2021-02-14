using System.Collections.Generic;
using System.Threading.Tasks;

namespace WDE.Conditions.Data
{
    public interface IConditionDataJsonProvider
    {
        string GetConditionsJson();
        string GetConditionSourcesJson();
        string GetConditionGroupsJson();

        void SaveConditions(string json);
        Task SaveConditionsAsync(string json);
        void SaveConditionSources(string json);
        Task SaveConditionSourcesAsync(string json);
        void SaveConditionGroups(string json);
        Task SaveConditionGroupsAsync(string json);
    }

    public interface IConditionDataProvider
    {
        IEnumerable<ConditionJsonData> GetConditions();
        IEnumerable<ConditionSourcesJsonData> GetConditionSources();
        IEnumerable<ConditionGroupsJsonData> GetConditionGroups();

        Task SaveConditions(List<ConditionJsonData> collection);
        Task SaveConditionSources(List<ConditionSourcesJsonData> collection);
        Task SaveConditionGroups(List<ConditionGroupsJsonData> collection);
    }
}