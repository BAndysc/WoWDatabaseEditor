using System.IO;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;

namespace WDE.TrinitySmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartDataJsonProvider : ISmartDataJsonProvider
    {
        public string GetActionsJson() => File.ReadAllText("SmartData/actions.json");

        public string GetEventsJson() => File.ReadAllText("SmartData/events.json");

        public string GetTargetsJson() => File.ReadAllText("SmartData/targets.json");

        public string GetEventsGroupsJson() => File.ReadAllText("SmartData/events_groups.json");

        public string GetActionsGroupsJson() => File.ReadAllText("SmartData/actions_groups.json");

        public string GetTargetsGroupsJson() => File.ReadAllText("SmartData/targets_groups.json");

        {
        }






    }
}