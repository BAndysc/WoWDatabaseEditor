using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
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
        private readonly IMessageBoxService messageBoxService;

        public SmartRawDataProvider(ISmartDataJsonProvider jsonProvider,
            ISmartDataSerializationProvider serializationProvider,
            IMessageBoxService messageBoxService)
        {
            this.jsonProvider = jsonProvider;
            this.serializationProvider = serializationProvider;
            this.messageBoxService = messageBoxService;
            actions = DeserializeData(jsonProvider.GetActionsJson(), "actions.json");
            events = DeserializeData(jsonProvider.GetEventsJson(), "events.json");
            targets = DeserializeData(jsonProvider.GetTargetsJson(), "targets.json");
            eventsGroups = DeserializeGroups(jsonProvider.GetEventsGroupsJson(), "events_groups.json");
            actionsGroups = DeserializeGroups(jsonProvider.GetActionsGroupsJson(), "actions_groups.json");
            targetsGroups = DeserializeGroups(jsonProvider.GetTargetsGroupsJson(), "targets_groups.json");
        }

        private List<SmartGenericJsonData> DeserializeData(string json, string fileName)
        {
            try
            {
                return serializationProvider.DeserializeSmartData<SmartGenericJsonData>(json);
            }
            catch (Exception e)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Error while loading smart data")
                    .SetMainInstruction("Smart data file is corrupted")
                    .SetContent("File " + fileName +
                                " is corrupted, either this is a faulty update or you have made a faulty change.\n\nThe SAI editor will not work correctly.\n\n"  + e.Message)
                    .WithOkButton(true)
                    .Build()).ListenErrors();
            }

            return new List<SmartGenericJsonData>();
        }

        private List<SmartGroupsJsonData> DeserializeGroups(string json, string fileName)
        {
            try
            {
                return serializationProvider.DeserializeSmartData<SmartGroupsJsonData>(json);
            }
            catch (Exception e)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Error while loading smart groups")
                    .SetMainInstruction("Smart groups file is corrupted")
                    .SetContent("File " + fileName +
                                " is corrupted, either this is a faulty update or you have made a faulty change.\n\nThe SAI editor will not work correctly.\n\n"  + e.Message)
                    .WithOkButton(true)
                    .Build()).ListenErrors();
            }

            return new List<SmartGroupsJsonData>();
        }

        public IEnumerable<SmartGenericJsonData> GetActions() => actions;
        public IEnumerable<SmartGenericJsonData> GetEvents() => events;
        public IEnumerable<SmartGenericJsonData> GetTargets() => targets;
        public IEnumerable<SmartGroupsJsonData> GetEventsGroups() => eventsGroups;
        public IEnumerable<SmartGroupsJsonData> GetActionsGroups() => actionsGroups;
        public IEnumerable<SmartGroupsJsonData> GetTargetsGroups() => targetsGroups;
    }
}