using Newtonsoft.Json;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    public class DbTableDataSerializationProvider : IDbTableDataSerializationProvider
    {
        public T DeserializeTableDefinition<T>(string json) => JsonConvert.DeserializeObject<T>(json);
        
        public string SerializeTableDefinition<T>(T definition)
        {
            var settings = CreateJsonSerializationSettings();
            return JsonConvert.SerializeObject(definition, Formatting.Indented, settings);
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