using System;
using System.IO;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [AutoRegister]
    public class ConditionDataJsonProvider : IConditionDataJsonProvider
    {
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IRuntimeDataService dataService;

        public ConditionDataJsonProvider(ICurrentCoreVersion currentCoreVersion,
            IRuntimeDataService dataService)
        {
            this.currentCoreVersion = currentCoreVersion;
            this.dataService = dataService;
        }
        
        public Task<string> GetConditionsJson() => ReadFileOrEmptyArray(currentCoreVersion.Current.ConditionFeatures.ConditionsFile);
        public Task<string> GetConditionSourcesJson() => ReadFileOrEmptyArray(currentCoreVersion.Current.ConditionFeatures.ConditionSourcesFile);
        public Task<string> GetConditionGroupsJson() => ReadFileOrEmptyArray(currentCoreVersion.Current.ConditionFeatures.ConditionGroupsFile);
       
        private async Task<string> ReadFileOrEmptyArray(string path)
        {
            if (await dataService.Exists(path))
                return await dataService.ReadAllText(path);
            LOG.LogError("File not found: " + path + ". Fallback to an empty list.");
            return "[]";
        }
    }
}