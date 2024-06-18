using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartRawDataProviderAsync : ISmartRawDataProviderAsync
    {
        private List<SmartGenericJsonData>? actions;
        private List<SmartGenericJsonData>? events;
        private List<SmartGenericJsonData>? targets;
        private List<SmartGroupsJsonData>? eventsGroups;
        private List<SmartGroupsJsonData>? actionsGroups;
        private List<SmartGroupsJsonData>? targetsGroups;

        private readonly ISmartDataJsonProviderAsync jsonProvider;
        private readonly ISmartDataSerializationProvider serializationProvider;
        private readonly IMainThread mainThread;
        private readonly IMessageBoxService messageBoxService;

        public event Action? DefinitionsChanged;

        public SmartRawDataProviderAsync(ISmartDataJsonProviderAsync jsonProvider,
            ISmartDataSerializationProvider serializationProvider,
            IMainThread mainThread,
            IMessageBoxService messageBoxService)
        {
            this.jsonProvider = jsonProvider;
            this.serializationProvider = serializationProvider;
            this.mainThread = mainThread;
            this.messageBoxService = messageBoxService;
            jsonProvider.SourceFilesChanged += OnSourceFilesChanged;
        }

        private IDisposable? pendingRefreh;

        private void OnSourceFilesChanged()
        {
            void RefreshJsons()
            {
                actions = null;
                events = null;
                targets = null;
                eventsGroups = null;
                actionsGroups = null;
                targetsGroups = null;
                pendingRefreh = null;
                DefinitionsChanged?.Invoke();
            }

            pendingRefreh?.Dispose();
            pendingRefreh = mainThread.Delay(RefreshJsons, TimeSpan.FromSeconds(0.5));
        }

        public async Task<IReadOnlyList<SmartGenericJsonData>> GetEvents()
        {
            if (events == null)
                events = DeserializeData(await jsonProvider.GetEventsJson(), "events.json");
            return events;
        }

        public async Task<IReadOnlyList<SmartGenericJsonData>> GetActions()
        {
            if (actions == null)
                actions = DeserializeData(await jsonProvider.GetActionsJson(), "actions.json");
            return actions;
        }

        public async Task<IReadOnlyList<SmartGenericJsonData>> GetTargets()
        {
            if (targets == null)
                targets = DeserializeData(await jsonProvider.GetTargetsJson(), "targets.json");
            return targets;
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetEventsGroups()
        {
            if (eventsGroups == null)
                eventsGroups = DeserializeGroups(await jsonProvider.GetEventsGroupsJson(), "events_groups.json");
            return eventsGroups;
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetActionsGroups()
        {
            if (actionsGroups == null)
                actionsGroups = DeserializeGroups(await jsonProvider.GetActionsGroupsJson(), "actions_groups.json");
            return actionsGroups;
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetTargetsGroups()
        {
            if (targetsGroups == null)
                targetsGroups = DeserializeGroups(await jsonProvider.GetTargetsGroupsJson(), "targets_groups.json");
            return targetsGroups;
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

    }
}