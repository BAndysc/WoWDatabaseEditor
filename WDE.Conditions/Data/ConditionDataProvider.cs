using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [AutoRegister]
    public class ConditionDataProvider : IConditionDataProvider
    {
        private List<ConditionJsonData> conditions;
        private List<ConditionSourcesJsonData> conditionSources;
        private List<ConditionGroupsJsonData> conditionGroups;

        private readonly IConditionDataJsonProvider provider;
        private readonly IConditionDataSerializationProvider serializationProvider;

        public ConditionDataProvider(IConditionDataJsonProvider provider, IConditionDataSerializationProvider serializationProvider)
        {
            this.provider = provider;
            this.serializationProvider = serializationProvider;
            
            conditions = serializationProvider.DeserializeConditionData<ConditionJsonData>(provider.GetConditionsJson());
            conditionSources = serializationProvider.DeserializeConditionData<ConditionSourcesJsonData>(provider.GetConditionSourcesJson());
            conditionGroups = serializationProvider.DeserializeConditionData<ConditionGroupsJsonData>(provider.GetConditionGroupsJson());
        }

        public IEnumerable<ConditionJsonData> GetConditions() => conditions;
        public IEnumerable<ConditionSourcesJsonData> GetConditionSources() => conditionSources;
        public IEnumerable<ConditionGroupsJsonData> GetConditionGroups() => conditionGroups;

        public async Task SaveConditions(List<ConditionJsonData> collection)
        {
            await provider.SaveConditionsAsync(serializationProvider.SerializeConditionData(collection));
            conditions = collection;
        }

        public async Task SaveConditionSources(List<ConditionSourcesJsonData> collection)
        {
            await provider.SaveConditionSourcesAsync(serializationProvider.SerializeConditionData(collection));
            conditionSources = collection;
        }

        public async Task SaveConditionGroups(List<ConditionGroupsJsonData> collection)
        {
            await provider.SaveConditionSourcesAsync(serializationProvider.SerializeConditionData(collection));
            conditionGroups = collection;
        }
    }
}