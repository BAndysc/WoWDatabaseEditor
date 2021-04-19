using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [UniqueProvider]
    public interface IDbTableDataSerializationProvider
    {
        T DeserializeTableDefinition<T>(string json);
        string SerializeTableDefinition<T>(T definition);
    }
}