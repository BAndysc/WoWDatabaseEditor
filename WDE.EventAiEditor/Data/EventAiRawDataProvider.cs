using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.EventAiEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class EventAiRawDataProvider : IEventAiRawDataProvider
    {
        private List<EventActionGenericJsonData> actions;
        private List<EventActionGenericJsonData> events;
        private List<EventAiGroupsJsonData> eventsGroups;
        private List<EventAiGroupsJsonData> actionsGroups;

        private readonly IEventAiDataJsonProvider jsonProvider;
        private readonly IEventAiDataSerializationProvider serializationProvider;

        public EventAiRawDataProvider(IEventAiDataJsonProvider jsonProvider,
            IEventAiDataSerializationProvider serializationProvider)
        {
            this.jsonProvider = jsonProvider;
            this.serializationProvider = serializationProvider;
            actions = serializationProvider.DeserializeData<EventActionGenericJsonData>(jsonProvider.GetActionsJson());
            events = serializationProvider.DeserializeData<EventActionGenericJsonData>(jsonProvider.GetEventsJson());
            eventsGroups = serializationProvider.DeserializeData<EventAiGroupsJsonData>(jsonProvider.GetEventsGroupsJson());
            actionsGroups = serializationProvider.DeserializeData<EventAiGroupsJsonData>(jsonProvider.GetActionsGroupsJson());
        }

        public IEnumerable<EventActionGenericJsonData> GetActions() => actions;
        public IEnumerable<EventActionGenericJsonData> GetEvents() => events;
        public IEnumerable<EventAiGroupsJsonData> GetEventsGroups() => eventsGroups;
        public IEnumerable<EventAiGroupsJsonData> GetActionsGroups() => actionsGroups;
    }
}