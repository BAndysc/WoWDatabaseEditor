using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartDataProvider : ISmartDataProvider
    {
        private readonly List<SmartGenericJsonData> actions;
        private readonly List<SmartGenericJsonData> events;
        private readonly List<SmartGenericJsonData> targets;
        private readonly List<SmartGroupsJsonData> eventsGroups;
        private readonly List<SmartGroupsJsonData> actionsGroups;
        private readonly List<SmartGroupsJsonData> targetsGroups;

        private readonly ICurrentCoreVersion coreVersion;

        public SmartDataProvider(ISmartRawDataProvider smartRawDataProvider,
            ICurrentCoreVersion coreVersion)
        {
            this.coreVersion = coreVersion;

            actions = smartRawDataProvider.GetActions().Where(IsSmartValidForCore).ToList();
            events = smartRawDataProvider.GetEvents().Where(IsSmartValidForCore).ToList();
            targets = smartRawDataProvider.GetTargets().Where(IsSmartValidForCore).ToList();

            var actionKeys = actions.Select(g => g.Name).ToHashSet();
            var eventKeys = events.Select(g => g.Name).ToHashSet();
            var targetKeys = targets.Select(g => g.Name).ToHashSet();

            eventsGroups = smartRawDataProvider.GetEventsGroups().Select(group =>
                    new SmartGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => eventKeys.Contains(name)).ToList()
                    })
                .ToList();
            
            actionsGroups = smartRawDataProvider.GetActionsGroups().Select(group =>
                    new SmartGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => actionKeys.Contains(name)).ToList()
                    })
                .ToList();
            
            targetsGroups = smartRawDataProvider.GetTargetsGroups().Select(group =>
                new SmartGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => targetKeys.Contains(name)).ToList()
                    })
                .ToList();
        }

        private bool IsSmartValidForCore(SmartGenericJsonData data)
        {
            return data.Tags == null || data.Tags.Contains(coreVersion.Current.Tag) || (coreVersion.Current.SmartScriptFeatures.ForceLoadTag != null && data.Tags.Contains(coreVersion.Current.SmartScriptFeatures.ForceLoadTag));
        }
        
        public IEnumerable<SmartGenericJsonData> GetActions() => actions;
        public IEnumerable<SmartGenericJsonData> GetEvents() => events;
        public IEnumerable<SmartGenericJsonData> GetTargets() => targets;
        public IEnumerable<SmartGroupsJsonData> GetEventsGroups() => eventsGroups;
        public IEnumerable<SmartGroupsJsonData> GetActionsGroups() => actionsGroups;
        public IEnumerable<SmartGroupsJsonData> GetTargetsGroups() => targetsGroups;
    }
}