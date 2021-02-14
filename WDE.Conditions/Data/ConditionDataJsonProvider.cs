using System.IO;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [AutoRegister]
    public class ConditionDataJsonProvider : IConditionDataJsonProvider
    {
        public string GetConditionsJson() => File.ReadAllText("SmartData/conditions.json");

        public string GetConditionSourcesJson() => File.ReadAllText("SmartData/condition_sources.json");
        public string GetConditionGroupsJson() => File.ReadAllText("SmartData/conditions_groups.json");
        
        public void SaveConditions(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/conditions.json"))
                    writer.Write(json);
            }
        }
        
        public async Task SaveConditionsAsync(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/conditions.json"))
                    await writer.WriteAsync(json);
            }
        }
        
        public void SaveConditionSources(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/condition_sources.json"))
                    writer.Write(json);
            }
        }
        
        public async Task SaveConditionSourcesAsync(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/condition_sources.json"))
                    await writer.WriteAsync(json);
            }
        }
        
        public void SaveConditionGroups(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/conditions_groups.json"))
                    writer.Write(json);
            }
        }
        
        public async Task SaveConditionGroupsAsync(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                using (StreamWriter writer = File.CreateText("SmartData/conditions_groups.json"))
                    await writer.WriteAsync(json);
            }
        }
    }
}