using System.IO;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [AutoRegister]
    public class ConditionDataJsonProvider : IConditionDataJsonProvider
    {
        public string GetConditionsJson()
        {
            return File.ReadAllText("SmartData/conditions.json");
        }

        public string GetConditionSourcesJson()
        {
            return File.ReadAllText("SmartData/condition_sources.json");
        }
    }
}