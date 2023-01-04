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
        private List<ConditionJsonData>? conditions;
        private List<ConditionSourcesJsonData>? conditionSources;
        private List<ConditionGroupsJsonData>? conditionGroups;

        private readonly IConditionDataJsonProvider provider;
        private readonly IConditionDataSerializationProvider serializationProvider;
        private readonly ICurrentCoreVersion currentCoreVersion;

        public ConditionDataProvider(IConditionDataJsonProvider provider, 
            IConditionDataSerializationProvider serializationProvider,
            ICurrentCoreVersion currentCoreVersion)
        {
            this.provider = provider;
            this.serializationProvider = serializationProvider;
            this.currentCoreVersion = currentCoreVersion;
        }

        public async Task<IReadOnlyList<ConditionJsonData>> GetConditions()
        {
            if (conditions == null)
            {
                var currentTag = currentCoreVersion.Current.Tag;
                conditions = serializationProvider
                    .DeserializeConditionData<ConditionJsonData>(await provider.GetConditionsJson())
                    .Where(c => c.Tags == null || c.Tags.Contains(currentTag))
                    .ToList();
            }
            return conditions;
        }

        public async Task<IReadOnlyList<ConditionSourcesJsonData>> GetConditionSources()
        {
            if (conditionSources == null)
            {
                conditionSources = serializationProvider.DeserializeConditionData<ConditionSourcesJsonData>(await provider.GetConditionSourcesJson());
            }
            return conditionSources;
        }

        public async Task<IReadOnlyList<ConditionGroupsJsonData>> GetConditionGroups()
        {
            if (conditionGroups == null)
            {
                conditionGroups = serializationProvider.DeserializeConditionData<ConditionGroupsJsonData>(await provider.GetConditionGroupsJson());
            }
            return conditionGroups;
        }
    }
}