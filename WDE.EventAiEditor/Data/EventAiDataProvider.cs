using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WDE.EventAiEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class EventAiDataProvider : IEventAiDataProvider
    {
        private readonly List<EventActionGenericJsonData> actions;
        private readonly List<EventActionGenericJsonData> events;
        private readonly List<EventAiGroupsJsonData> eventsGroups;
        private readonly List<EventAiGroupsJsonData> actionsGroups;

        private readonly ICurrentCoreVersion coreVersion;

        public EventAiDataProvider(IEventAiRawDataProvider eventAiRawDataProvider,
            ICurrentCoreVersion coreVersion)
        {
            this.coreVersion = coreVersion;

            actions = eventAiRawDataProvider.GetActions().Where(IsEventOrActionValidForCore).ToList();
            events = eventAiRawDataProvider.GetEvents().Where(IsEventOrActionValidForCore).ToList();

            var actionKeys = actions.Select(g => g.Name).ToHashSet();
            var eventKeys = events.Select(g => g.Name).ToHashSet();

            eventsGroups = eventAiRawDataProvider.GetEventsGroups().Select(group =>
                    new EventAiGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => eventKeys.Contains(name)).ToList()
                    })
                .ToList();
            
            actionsGroups = eventAiRawDataProvider.GetActionsGroups().Select(group =>
                    new EventAiGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => actionKeys.Contains(name)).ToList()
                    })
                .ToList();
        }

        private bool IsEventOrActionValidForCore(EventActionGenericJsonData data)
        {
            return data.Tags == null || data.Tags.Contains(coreVersion.Current.Tag);
        }
        
        public IEnumerable<EventActionGenericJsonData> GetActions() => actions;
        public IEnumerable<EventActionGenericJsonData> GetEvents() => events;
        public IEnumerable<EventAiGroupsJsonData> GetEventsGroups() => eventsGroups;
        public IEnumerable<EventAiGroupsJsonData> GetActionsGroups() => actionsGroups;
    }
}