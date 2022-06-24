using System.Collections.Generic;
using Newtonsoft.Json;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [AutoRegister]
    class ConditionDataSerializationProvider : IConditionDataSerializationProvider
    {
        public List<T> DeserializeConditionData<T>(string json) => JsonConvert.DeserializeObject<List<T>>(json)!;

        public string SerializeConditionData<T>(List<T> dataCollection) => JsonConvert.SerializeObject(dataCollection, Formatting.Indented);
    }
}
