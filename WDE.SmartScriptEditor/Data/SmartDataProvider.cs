using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartDataProviderAsync : ISmartDataProviderAsync
    {
        private List<SmartGenericJsonData>? actions;
        private List<SmartGenericJsonData>? events;
        private List<SmartGenericJsonData>? targets;
        private List<SmartGroupsJsonData>? eventsGroups;
        private List<SmartGroupsJsonData>? actionsGroups;
        private List<SmartGroupsJsonData>? targetsGroups;

        private readonly ISmartRawDataProviderAsync smartRawDataProvider;
        private readonly ICurrentCoreVersion coreVersion;

        public SmartDataProviderAsync(ISmartRawDataProviderAsync smartRawDataProvider,
            ICurrentCoreVersion coreVersion)
        {
            this.smartRawDataProvider = smartRawDataProvider;
            this.coreVersion = coreVersion;

            LoadAsync().ListenErrors();
        }

        private async Task LoadAsync()
        {
            actions = (await smartRawDataProvider.GetActions()).Where(IsSmartValidForCore).ToList();
            events = (await smartRawDataProvider.GetEvents()).Where(IsSmartValidForCore).ToList();
            targets = (await smartRawDataProvider.GetTargets()).Where(IsSmartValidForCore).ToList();

            var actionKeys = actions.Select(g => g.Name).ToHashSet();
            var eventKeys = events.Select(g => g.Name).ToHashSet();
            var targetKeys = targets.Select(g => g.Name).ToHashSet();

            eventsGroups = (await smartRawDataProvider.GetEventsGroups()).Select(group =>
                    new SmartGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => eventKeys.Contains(name)).ToList()
                    })
                .ToList();
            
            actionsGroups = (await smartRawDataProvider.GetActionsGroups()).Select(group =>
                    new SmartGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => actionKeys.Contains(name)).ToList()
                    })
                .ToList();
            
            targetsGroups = (await smartRawDataProvider.GetTargetsGroups()).Select(group =>
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

        public async Task<IReadOnlyList<SmartGenericJsonData>> GetEvents()
        {
            if (events == null)
                await LoadAsync();
            return events!;
        }

        public async Task<IReadOnlyList<SmartGenericJsonData>> GetActions()
        {
            if (actions == null)
                await LoadAsync();
            return actions!;
        }

        public async Task<IReadOnlyList<SmartGenericJsonData>> GetTargets()
        {
            if (targets == null)
                await LoadAsync();
            return targets!;
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetEventsGroups()
        {
            if (eventsGroups == null)
                await LoadAsync();
            return eventsGroups!;
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetActionsGroups()
        {
            if (actionsGroups == null)
                await LoadAsync();
            return actionsGroups!;
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetTargetsGroups()
        {
            if (targetsGroups == null)
                await LoadAsync();
            return targetsGroups!;
        }
    }
}