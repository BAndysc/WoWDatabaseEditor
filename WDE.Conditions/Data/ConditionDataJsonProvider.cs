using System;
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
        
        public string GetConditionsJson() => ReadFileOrEmptyArray(currentCoreVersion.Current.ConditionFeatures.ConditionsFile);
        public string GetConditionSourcesJson() => ReadFileOrEmptyArray(currentCoreVersion.Current.ConditionFeatures.ConditionSourcesFile);
        public string GetConditionGroupsJson() => ReadFileOrEmptyArray(currentCoreVersion.Current.ConditionFeatures.ConditionGroupsFile);
       
        private string ReadFileOrEmptyArray(string path)
        {
            if (File.Exists(path))  
                return File.ReadAllText(path);
            Console.WriteLine("File not found: " + path + ". Fallback to an empty list.");
            return "[]";
        }
    }
}