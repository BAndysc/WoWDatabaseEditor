using System.Collections.Generic;

namespace WDE.EventAiEditor.Data
{
    public interface IEventAiDataSerializationProvider
    {
        List<T> DeserializeData<T>(string json);
        string SerializeData<T>(List<T> dataCollection);
    }
}
