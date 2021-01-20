using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Conditions.Data
{
    [UniqueProvider]
    public interface IConditionDataSerializationProvider
    {
        List<T> DeserializeConditionData<T>(string json);
        string SerializeConditionData<T>(List<T> dataCollection);
    }
}
