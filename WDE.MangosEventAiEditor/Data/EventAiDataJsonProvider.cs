using System.IO;
using WDE.Common.CoreVersion;
using WDE.EventAiEditor.Data;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class EventAiDataJsonProvider : IEventAiDataJsonProvider
    {
        private readonly ICurrentCoreVersion currentCoreVersion;

        public EventAiDataJsonProvider(ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
        }

        private IEventAiFeatures EventAiFeatures => currentCoreVersion.Current.EventAiFeatures;
        
        public string GetActionsJson() => File.ReadAllText(EventAiFeatures.ActionsPath ?? "EventAiData/actions.json");

        public string GetEventsJson() => File.ReadAllText(EventAiFeatures.EventsPath ?? "EventAiData/events.json");

        public string GetEventsGroupsJson() => File.ReadAllText(EventAiFeatures.EventGroupPath ?? "EventAiData/events_groups.json");

        public string GetActionsGroupsJson() => File.ReadAllText(EventAiFeatures.ActionGroupPath ?? "EventAiData/actions_groups.json");
    }
}