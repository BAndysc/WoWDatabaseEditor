using System.IO;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [AutoRegister]
    public class ConditionDataJsonProvider : IConditionDataJsonProvider
    {
        public string GetConditionsJson() => File.ReadAllText("SmartData/conditions.json");

        public string GetConditionSourcesJson() => File.ReadAllText("SmartData/condition_sources.json");
        public string GetConditionGroupsJson() => File.ReadAllText("SmartData/conditions_groups.json");
    }
}