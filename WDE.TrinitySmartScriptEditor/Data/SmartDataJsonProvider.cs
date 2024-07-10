using System;
using System.IO;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;

namespace WDE.TrinitySmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartDataJsonProviderAsync : ISmartDataJsonProviderAsync
    {
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IRuntimeDataService runtimeDataService;

        public SmartDataJsonProviderAsync(ICurrentCoreVersion currentCoreVersion, 
            IRuntimeDataService runtimeDataService)
        {
            this.currentCoreVersion = currentCoreVersion;
            this.runtimeDataService = runtimeDataService;
        }

        private ISmartScriptFeatures smartScriptFeatures => currentCoreVersion.Current.SmartScriptFeatures;
        
        public Task<string> GetActionsJson() => runtimeDataService.ReadAllText(smartScriptFeatures.ActionsPath ?? "SmartData/actions.json");
        public Task<string> GetEventsJson() => runtimeDataService.ReadAllText(smartScriptFeatures.EventsPath ??"SmartData/events.json");
        public Task<string> GetTargetsJson() => runtimeDataService.ReadAllText(smartScriptFeatures.TargetsPath ??"SmartData/targets.json");
        public Task<string> GetEventsGroupsJson() => runtimeDataService.ReadAllText(smartScriptFeatures.EventGroupPath ??"SmartData/events_groups.json");
        public Task<string> GetActionsGroupsJson() => runtimeDataService.ReadAllText(smartScriptFeatures.ActionGroupPath ??"SmartData/actions_groups.json");
        public Task<string> GetTargetsGroupsJson() => runtimeDataService.ReadAllText(smartScriptFeatures.TargetGroupPath ??"SmartData/targets_groups.json");
        public event Action? SourceFilesChanged;
    }
}