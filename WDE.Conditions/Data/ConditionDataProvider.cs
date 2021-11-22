using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
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

        public ConditionDataProvider(IConditionDataJsonProvider provider, 
            IConditionDataSerializationProvider serializationProvider,
            ICurrentCoreVersion currentCoreVersion)
        {
            this.provider = provider;
            this.serializationProvider = serializationProvider;

            var currentTag = currentCoreVersion.Current.Tag;

            conditions = serializationProvider
                .DeserializeConditionData<ConditionJsonData>(provider.GetConditionsJson())
                .Where(c => c.Tags == null || c.Tags.Contains(currentTag))
                .ToList();
            conditionSources = serializationProvider.DeserializeConditionData<ConditionSourcesJsonData>(provider.GetConditionSourcesJson());
            conditionGroups = serializationProvider.DeserializeConditionData<ConditionGroupsJsonData>(provider.GetConditionGroupsJson());
        }

        public IEnumerable<ConditionJsonData> GetConditions() => conditions;
        public IEnumerable<ConditionSourcesJsonData> GetConditionSources() => conditionSources;
        public IEnumerable<ConditionGroupsJsonData> GetConditionGroups() => conditionGroups;
    }
}