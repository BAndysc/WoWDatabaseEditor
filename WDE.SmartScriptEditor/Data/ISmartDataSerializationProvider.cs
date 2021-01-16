using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Data
{
    [UniqueProvider]
    public interface ISmartDataSerializationProvider
    {
        List<T> DeserializeSmartData<T>(string json);
        string SerializeSmartData<T>(List<T> dataCollection);
    }
}
