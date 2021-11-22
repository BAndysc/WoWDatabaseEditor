using System.IO;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [AutoRegister]
    public class ConditionDataJsonProvider : IConditionDataJsonProvider
    {
        private readonly ICurrentCoreVersion currentCoreVersion;

        public ConditionDataJsonProvider(ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
        }
        
        public string GetConditionsJson() => File.ReadAllText(currentCoreVersion.Current.ConditionFeatures.ConditionsFile);
        public string GetConditionSourcesJson() => File.ReadAllText(currentCoreVersion.Current.ConditionFeatures.ConditionSourcesFile);
        public string GetConditionGroupsJson() => File.ReadAllText(currentCoreVersion.Current.ConditionFeatures.ConditionGroupsFile);
    }
}