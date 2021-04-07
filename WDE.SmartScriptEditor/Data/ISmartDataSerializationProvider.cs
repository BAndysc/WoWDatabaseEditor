using System.Collections.Generic;

namespace WDE.SmartScriptEditor.Data
{
    public interface ISmartDataSerializationProvider
    {
        List<T> DeserializeSmartData<T>(string json);
        string SerializeSmartData<T>(List<T> dataCollection);
    }
}
