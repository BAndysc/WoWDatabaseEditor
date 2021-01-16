using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using Newtonsoft.Json;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    class SmartDataSerializationProvider: ISmartDataSerializationProvider
    {
        public List<T> DeserializeSmartData<T>(string json) => JsonConvert.DeserializeObject<List<T>>(json);

        public string SerializeSmartData<T>(List<T> dataCollection)
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
