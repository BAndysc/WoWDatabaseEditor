using System.IO;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;

namespace WDE.TrinitySmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartDataJsonProvider : ISmartDataJsonProvider
    {
        private readonly ICurrentCoreVersion currentCoreVersion;

        public SmartDataJsonProvider(ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
        }

        private ISmartScriptFeatures smartScriptFeatures => currentCoreVersion.Current.SmartScriptFeatures;
        
        public string GetActionsJson() => File.ReadAllText(smartScriptFeatures.ActionsPath ?? "SmartData/actions.json");

        public string GetEventsJson() => File.ReadAllText(smartScriptFeatures.EventsPath ??"SmartData/events.json");

        public string GetTargetsJson() => File.ReadAllText(smartScriptFeatures.TargetsPath ??"SmartData/targets.json");

        public string GetEventsGroupsJson() => File.ReadAllText(smartScriptFeatures.EventGroupPath ??"SmartData/events_groups.json");

        public string GetActionsGroupsJson() => File.ReadAllText(smartScriptFeatures.ActionGroupPath ??"SmartData/actions_groups.json");

        public string GetTargetsGroupsJson() => File.ReadAllText(smartScriptFeatures.TargetGroupPath ??"SmartData/targets_groups.json");
    }
}