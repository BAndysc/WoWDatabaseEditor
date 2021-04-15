using System;
using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class DbTableDefinitionProvider : IDbTableDefinitionProvider
    {
        private readonly Dictionary<string, DatabaseEditorTableDefinitionJson> definitions = new();
        
        public DbTableDefinitionProvider(IDbTableDataSerializationProvider serializationProvider, IDbTableDataJsonProvider jsonProvider)
        {
            foreach (var source in jsonProvider.GetDefinitionSources())
            {
                var definition =
                    serializationProvider.DeserializeTableDefinition<DatabaseEditorTableDefinitionJson>(source);
                definitions[definition.TableName] = definition;
            }
        }

        public DatabaseEditorTableDefinitionJson? GetDefinition(string tableName)
        {
            if (tableName != null && definitions.TryGetValue(tableName, out var definition))
                return definition;
            return null;
        }
    }
}