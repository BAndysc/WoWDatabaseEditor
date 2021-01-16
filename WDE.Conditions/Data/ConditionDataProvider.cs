using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [AutoRegister]
    public class ConditionDataProvider : IConditionDataProvider
    {
        private readonly List<ConditionJsonData> conditions;
        private readonly List<ConditionSourcesJsonData> condition_sources;
        private readonly List<ConditionGroupsJsonData> conditionGroups;

        public ConditionDataProvider(IConditionDataJsonProvider provider, IConditionDataSerializationProvider serializationProvider)
        {
            conditions = serializationProvider.DeserializeConditionData<ConditionJsonData>(provider.GetConditionsJson());
            condition_sources = serializationProvider.DeserializeConditionData<ConditionSourcesJsonData>(provider.GetConditionSourcesJson());
            conditionGroups = serializationProvider.DeserializeConditionData<ConditionGroupsJsonData>(provider.GetConditionGroupsJson());
        }

        public IEnumerable<ConditionJsonData> GetConditions() => conditions;
        public IEnumerable<ConditionSourcesJsonData> GetConditionSources() => condition_sources;
        public IEnumerable<ConditionGroupsJsonData> GetConditionGroups() => conditionGroups;
    }
}