using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WDE.SmartScriptEditor.Data
{
    public interface ISmartDataJsonProvider
    {
        string GetEventsJson();
        string GetActionsJson();
        string GetTargetsJson();
        string GetEventsGroupsJson();
        string GetActionsGroupsJson();
        string GetTargetsGroupsJson();

        void SaveEvents(string json);
        Task SaveEventsAsync(string json);
        void SaveActions(string json);
        Task SaveActionsAsync(string json);
        void SaveTargets(string json);
        Task SaveTargetsAsync(string json);

        void SaveEventsGroups(string json);
        Task SaveEventsGroupsAsync(string json);
        void SaveActionsGroups(string json);
        Task SaveActionsGroupsAsync(string json);
        void SaveTargetsGroups(string json);
        Task SaveTargetsGroupsAsync(string json);
    }
    
    public interface ISmartRawDataProvider
    {
        IEnumerable<SmartGenericJsonData> GetEvents();
        IEnumerable<SmartGenericJsonData> GetActions();
        IEnumerable<SmartGenericJsonData> GetTargets();
        IEnumerable<SmartGroupsJsonData> GetEventsGroups();
        IEnumerable<SmartGroupsJsonData> GetActionsGroups();
        IEnumerable<SmartGroupsJsonData> GetTargetsGroups();

        Task SaveEvents(List<SmartGenericJsonData> events);
        Task SaveActions(List<SmartGenericJsonData> actions);
        Task SaveTargets(List<SmartGenericJsonData> targets);
        Task SaveEventGroups(List<SmartGroupsJsonData> groups);
        Task SaveActionsGroups(List<SmartGroupsJsonData> groups);
        Task SaveTargetsGroups(List<SmartGroupsJsonData> groups);
    }
    
    public interface ISmartDataProvider
    {
        IEnumerable<SmartGenericJsonData> GetEvents();
        IEnumerable<SmartGenericJsonData> GetActions();
        IEnumerable<SmartGenericJsonData> GetTargets();
        IEnumerable<SmartGroupsJsonData> GetEventsGroups();
        IEnumerable<SmartGroupsJsonData> GetActionsGroups();
        IEnumerable<SmartGroupsJsonData> GetTargetsGroups();
    }

    public interface ISmartTypeListProvider
    {
        Task<(int, bool)?> Get(SmartType type, Func<SmartGenericJsonData, bool> predicate, List<(int, string)>? customItems = null);
    }
}