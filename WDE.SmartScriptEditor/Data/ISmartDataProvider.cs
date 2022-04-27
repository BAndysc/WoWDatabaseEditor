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
    }
    
    public interface ISmartRawDataProvider
    {
        IEnumerable<SmartGenericJsonData> GetEvents();
        IEnumerable<SmartGenericJsonData> GetActions();
        IEnumerable<SmartGenericJsonData> GetTargets();
        IEnumerable<SmartGroupsJsonData> GetEventsGroups();
        IEnumerable<SmartGroupsJsonData> GetActionsGroups();
        IEnumerable<SmartGroupsJsonData> GetTargetsGroups();
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