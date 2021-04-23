using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces
{
    [UniqueProvider]
    public interface ITableDefinitionDeserializer
    {
        T DeserializeTableDefinition<T>(string json);
    }
}