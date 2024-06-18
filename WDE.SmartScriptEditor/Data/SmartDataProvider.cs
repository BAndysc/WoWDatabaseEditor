using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Modules;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartDataProviderAsync : ISmartDataProviderAsync, IGlobalAsyncInitializer
    {
        private List<SmartGenericJsonData>? actions;
        private List<SmartGenericJsonData>? events;
        private List<SmartGenericJsonData>? targets;
        private List<SmartGroupsJsonData>? eventsGroups;
        private List<SmartGroupsJsonData>? actionsGroups;
        private List<SmartGroupsJsonData>? targetsGroups;

        private readonly ISmartRawDataProviderAsync smartRawDataProvider;
        private readonly ICurrentCoreVersion coreVersion;

        private CancellationTokenSource? pendingLoadToken;

        public event Action? DefinitionsChanged;

        public SmartDataProviderAsync(ISmartRawDataProviderAsync smartRawDataProvider,
            ICurrentCoreVersion coreVersion)
        {
            this.smartRawDataProvider = smartRawDataProvider;
            this.coreVersion = coreVersion;
            smartRawDataProvider.DefinitionsChanged += () =>
            {
                isLoaded = false;
                DoInitialize(true).ListenErrors();
            };
        }

        private bool isLoaded;

        public async Task Initialize()
        {
            await DoInitialize(false);
        }

        private async Task DoInitialize(bool invokeEvent)
        {
            pendingLoadToken?.Cancel();
            pendingLoadToken = new();
            await LoadAsync(invokeEvent, pendingLoadToken.Token);
        }

        private async Task LoadAsync(bool invokeEvent, CancellationToken token)
        {
            if (isLoaded)
                return;

            isLoaded = true;
            var actions = (await smartRawDataProvider.GetActions()).Where(IsSmartValidForCore).ToList();
            var events = (await smartRawDataProvider.GetEvents()).Where(IsSmartValidForCore).ToList();
            var targets = (await smartRawDataProvider.GetTargets()).Where(IsSmartValidForCore).ToList();

            var actionKeys = actions.Select(g => g.Name).ToHashSet();
            var eventKeys = events.Select(g => g.Name).ToHashSet();
            var targetKeys = targets.Select(g => g.Name).ToHashSet();

            var eventsGroups = (await smartRawDataProvider.GetEventsGroups()).Select(group =>
                    new SmartGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => eventKeys.Contains(name)).ToList()
                    })
                .ToList();
            
            var actionsGroups = (await smartRawDataProvider.GetActionsGroups()).Select(group =>
                    new SmartGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => actionKeys.Contains(name)).ToList()
                    })
                .ToList();
            
            var targetsGroups = (await smartRawDataProvider.GetTargetsGroups()).Select(group =>
                    new SmartGroupsJsonData()
                    {
                        Name = group.Name,
                        Members = group.Members.Where(name => targetKeys.Contains(name)).ToList()
                    })
                .ToList();

            if (token.IsCancellationRequested)
                return;

            this.actions = actions;
            this.events = events;
            this.targets = targets;
            this.eventsGroups = eventsGroups;
            this.actionsGroups = actionsGroups;
            this.targetsGroups = targetsGroups;
            if (invokeEvent)
                DefinitionsChanged?.Invoke();
        }

        private bool IsSmartValidForCore(SmartGenericJsonData data)
        {
            return data.Tags == null || data.Tags.Contains(coreVersion.Current.Tag) || (coreVersion.Current.SmartScriptFeatures.ForceLoadTag != null && data.Tags.Contains(coreVersion.Current.SmartScriptFeatures.ForceLoadTag));
        }

        public async Task<IReadOnlyList<SmartGenericJsonData>> GetEvents()
        {
            if (events == null)
                await Initialize();
            return events!;
        }

        public async Task<IReadOnlyList<SmartGenericJsonData>> GetActions()
        {
            if (actions == null)
                await Initialize();
            return actions!;
        }

        public async Task<IReadOnlyList<SmartGenericJsonData>> GetTargets()
        {
            if (targets == null)
                await Initialize();
            return targets!;
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetEventsGroups()
        {
            if (eventsGroups == null)
                await Initialize();
            return eventsGroups!;
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetActionsGroups()
        {
            if (actionsGroups == null)
                await Initialize();
            return actionsGroups!;
        }

        public async Task<IReadOnlyList<SmartGroupsJsonData>> GetTargetsGroups()
        {
            if (targetsGroups == null)
                await Initialize();
            return targetsGroups!;
        }
    }
}