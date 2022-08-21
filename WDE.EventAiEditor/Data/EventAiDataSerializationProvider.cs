using System.Collections.Generic;
using Newtonsoft.Json;
using WDE.Module.Attributes;

namespace WDE.EventAiEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class EventAiDataSerializationProvider : IEventAiDataSerializationProvider
    {
        public List<T> DeserializeData<T>(string json) => JsonConvert.DeserializeObject<List<T>>(json)!;

        public string SerializeData<T>(List<T> dataCollection)
        {
            var settings = CreateJsonSerializationSettings();
            return JsonConvert.SerializeObject(dataCollection, Formatting.Indented, settings);
        }

        private JsonSerializerSettings CreateJsonSerializationSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.DefaultValueHandling = DefaultValueHandling.Ignore;
            return settings;
        }
    }
}
