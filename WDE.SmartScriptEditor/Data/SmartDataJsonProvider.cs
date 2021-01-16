using System.IO;
using WDE.Module.Attributes;
using System.Threading.Tasks;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    public class SmartDataJsonProvider : ISmartDataJsonProvider
    {
        public string GetActionsJson() => File.ReadAllText("SmartData/actions.json");

        public string GetEventsJson() => File.ReadAllText("SmartData/events.json");

        public string GetTargetsJson() => File.ReadAllText("SmartData/targets.json");

        public string GetEventsGroupsJson() => File.ReadAllText("SmartData/events_groups.json");

        public string GetActionsGroupsJson() => File.ReadAllText("SmartData/actions_groups.json");

        public string GetTargetsGroupsJson() => File.ReadAllText("SmartData/targets_groups.json");

        public void SaveEvents(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/events.json"))
                    writer.Write(json);
            }
        }

        public async Task SaveEventsAsync(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/events.json"))
                    await writer.WriteAsync(json);
            }
        }
        public void SaveActions(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/actions.json"))
                    writer.Write(json);
            }
        }
        public async Task SaveActionsAsync(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/actions.json"))
                    await writer.WriteAsync(json);
            }
        }
        public void SaveTargets(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/targets.json"))
                    writer.Write(json);
            }
        }
        public async Task SaveTargetsAsync(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/targets.json"))
                    await writer.WriteAsync(json);
            }
        }

        public void SaveEventsGroups(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/events_groups.json"))
                    writer.Write(json);
            }
        }

        public async Task SaveEventsGroupsAsync(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/events_groups.json"))
                    await writer.WriteAsync(json);
            }
        }

        public void SaveActionsGroups(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/actions_groups.json"))
                    writer.Write(json);
            }
        }

        public async Task SaveActionsGroupsAsync(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/actions_groups.json"))
                    await writer.WriteAsync(json);
            }
        }

        public void SaveTargetsGroups(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/targets_groups.json"))
                    writer.Write(json);
            }
        }

        public async Task SaveTargetsGroupsAsync(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/targets_groups.json"))
                    await writer.WriteAsync(json);
            }
        }
    }
}