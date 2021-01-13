using System.Collections.Generic;
using Newtonsoft.Json;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [AutoRegister]
    public class ConditionDataProvider : IConditionDataProvider
    {
        private readonly List<ConditionJsonData> conditions;
        private readonly List<ConditionSourcesJsonData> condition_sources;

        public ConditionDataProvider(IConditionDataJsonProvider provider)
        {
            conditions = JsonConvert.DeserializeObject<List<ConditionJsonData>>(provider.GetConditionsJson());
            condition_sources = JsonConvert.DeserializeObject<List<ConditionSourcesJsonData>>(provider.GetConditionSourcesJson());
        }

        public IEnumerable<ConditionJsonData> GetConditions()
        {
            return conditions;
        }

        public IEnumerable<ConditionSourcesJsonData> GetConditionSources()
        {
            return condition_sources;
        }
    }
}