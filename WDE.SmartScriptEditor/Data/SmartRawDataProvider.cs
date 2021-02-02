using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartRawDataProvider : ISmartRawDataProvider
    {
        private List<SmartGenericJsonData> actions;
        private List<SmartGenericJsonData> events;
        private List<SmartGenericJsonData> targets;
        private List<SmartGroupsJsonData> eventsGroups;
        private List<SmartGroupsJsonData> actionsGroups;
        private List<SmartGroupsJsonData> targetsGroups;

        private readonly ISmartDataJsonProvider jsonProvider;
        private readonly ISmartDataSerializationProvider serializationProvider;

        public SmartRawDataProvider(ISmartDataJsonProvider jsonProvider,
            ISmartDataSerializationProvider serializationProvider)
        {
            this.jsonProvider = jsonProvider;
            this.serializationProvider = serializationProvider;
            actions = serializationProvider.DeserializeSmartData<SmartGenericJsonData>(jsonProvider.GetActionsJson());
            events = serializationProvider.DeserializeSmartData<SmartGenericJsonData>(jsonProvider.GetEventsJson());
            targets = serializationProvider.DeserializeSmartData<SmartGenericJsonData>(jsonProvider.GetTargetsJson());
            eventsGroups = serializationProvider.DeserializeSmartData<SmartGroupsJsonData>(jsonProvider.GetEventsGroupsJson());
            actionsGroups = serializationProvider.DeserializeSmartData<SmartGroupsJsonData>(jsonProvider.GetActionsGroupsJson());
            targetsGroups = serializationProvider.DeserializeSmartData<SmartGroupsJsonData>(jsonProvider.GetTargetsGroupsJson());
        }

        public IEnumerable<SmartGenericJsonData> GetActions() => actions;
        public IEnumerable<SmartGenericJsonData> GetEvents() => events;
        public IEnumerable<SmartGenericJsonData> GetTargets() => targets;
        public IEnumerable<SmartGroupsJsonData> GetEventsGroups() => eventsGroups;
        public IEnumerable<SmartGroupsJsonData> GetActionsGroups() => actionsGroups;
        public IEnumerable<SmartGroupsJsonData> GetTargetsGroups() => targetsGroups;

        public async Task SaveEvents(List<SmartGenericJsonData> events)
        {
            await jsonProvider.SaveEventsAsync(serializationProvider.SerializeSmartData(events));
            this.events = events;
        }
        
        public async Task SaveActions(List<SmartGenericJsonData> actions)
        {
            await jsonProvider.SaveActionsAsync(serializationProvider.SerializeSmartData(actions));
            this.actions = actions;
        }
        
        public async Task SaveTargets(List<SmartGenericJsonData> targets)
        {
            await jsonProvider.SaveTargetsAsync(serializationProvider.SerializeSmartData(targets));
            this.targets = targets;
        }
        
        public async Task SaveEventGroups(List<SmartGroupsJsonData> groups)
        {
            await jsonProvider.SaveEventsGroupsAsync(serializationProvider.SerializeSmartData(groups));
            eventsGroups = groups;
        }
        
        public async Task SaveActionsGroups(List<SmartGroupsJsonData> groups)
        {
            await jsonProvider.SaveActionsGroupsAsync(serializationProvider.SerializeSmartData(groups));
            actionsGroups = groups;
        }
        
        public async Task SaveTargetsGroups(List<SmartGroupsJsonData> groups)
        {
            await jsonProvider.SaveTargetsGroupsAsync(serializationProvider.SerializeSmartData(groups));
            targetsGroups = groups;
        }
    }
}