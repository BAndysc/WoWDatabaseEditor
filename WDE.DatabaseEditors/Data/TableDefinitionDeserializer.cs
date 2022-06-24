using Newtonsoft.Json;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    public class TableDefinitionDeserializer : ITableDefinitionDeserializer
    {
        public T DeserializeTableDefinition<T>(string json) => JsonConvert.DeserializeObject<T>(json)!;
    }
}