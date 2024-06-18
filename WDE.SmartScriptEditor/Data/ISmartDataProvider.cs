using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WDE.SmartScriptEditor.Data
{
    public interface ISmartDataJsonProviderAsync
    {
        Task<string> GetEventsJson();
        Task<string> GetActionsJson();
        Task<string> GetTargetsJson();
        Task<string> GetEventsGroupsJson();
        Task<string> GetActionsGroupsJson();
        Task<string> GetTargetsGroupsJson();
        event Action? SourceFilesChanged;
    }

    public interface ISmartRawDataProviderAsync
    {
        Task<IReadOnlyList<SmartGenericJsonData>> GetEvents();
        Task<IReadOnlyList<SmartGenericJsonData>> GetActions();
        Task<IReadOnlyList<SmartGenericJsonData>> GetTargets();
        Task<IReadOnlyList<SmartGroupsJsonData>> GetEventsGroups();
        Task<IReadOnlyList<SmartGroupsJsonData>> GetActionsGroups();
        Task<IReadOnlyList<SmartGroupsJsonData>> GetTargetsGroups();
        event Action? DefinitionsChanged;
    }
 
    public interface ISmartDataProviderAsync
    {
        Task<IReadOnlyList<SmartGenericJsonData>> GetEvents();
        Task<IReadOnlyList<SmartGenericJsonData>> GetActions();
        Task<IReadOnlyList<SmartGenericJsonData>> GetTargets();
        Task<IReadOnlyList<SmartGroupsJsonData>> GetEventsGroups();
        Task<IReadOnlyList<SmartGroupsJsonData>> GetActionsGroups();
        Task<IReadOnlyList<SmartGroupsJsonData>> GetTargetsGroups();
        event Action? DefinitionsChanged;
    }

    public interface ISmartTypeListProvider
    {
        Task<(int, bool)?> Get(SmartType type, Func<SmartGenericJsonData, bool> predicate, List<(int, string)>? customItems = null);
    }
}