using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WDE.EventAiEditor.Data
{
    public interface IEventAiDataJsonProvider
    {
        string GetEventsJson();
        string GetActionsJson();
        string GetEventsGroupsJson();
        string GetActionsGroupsJson();
    }
    
    public interface IEventAiRawDataProvider
    {
        IEnumerable<EventActionGenericJsonData> GetEvents();
        IEnumerable<EventActionGenericJsonData> GetActions();
        IEnumerable<EventAiGroupsJsonData> GetEventsGroups();
        IEnumerable<EventAiGroupsJsonData> GetActionsGroups();
    }
    
    public interface IEventAiDataProvider
    {
        IEnumerable<EventActionGenericJsonData> GetEvents();
        IEnumerable<EventActionGenericJsonData> GetActions();
        IEnumerable<EventAiGroupsJsonData> GetEventsGroups();
        IEnumerable<EventAiGroupsJsonData> GetActionsGroups();
    }

    public interface IEventActionListProvider
    {
        Task<(uint, bool)?> Get(EventOrAction type, Func<EventActionGenericJsonData, bool> predicate, List<(uint, string)>? customItems = null);
    }
}