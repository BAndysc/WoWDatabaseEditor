using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class TableDefinitionProvider : ITableDefinitionProvider
    {
        private readonly Dictionary<string, DatabaseTableDefinitionJson> definitions = new();
        
        public TableDefinitionProvider(ITableDefinitionDeserializer serializationProvider, ITableDefinitionJsonProvider jsonProvider)
        {
            foreach (var source in jsonProvider.GetDefinitionSources())
            {
                var definition =
                    serializationProvider.DeserializeTableDefinition<DatabaseTableDefinitionJson>(source);
                definitions[definition.TableName] = definition;
            }
        }

        public DatabaseTableDefinitionJson? GetDefinition(string tableName)
        {
            if (tableName != null && definitions.TryGetValue(tableName, out var definition))
                return definition;
            return null;
        }
    }
}