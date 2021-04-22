using System.Collections.Generic;
using WDE.Common.CoreVersion;
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
        
        public TableDefinitionProvider(ITableDefinitionDeserializer serializationProvider,
            ITableDefinitionJsonProvider jsonProvider,
            ICurrentCoreVersion currentCoreVersion)
        {
            foreach (var source in jsonProvider.GetDefinitionSources())
            {
                var definition =
                    serializationProvider.DeserializeTableDefinition<DatabaseTableDefinitionJson>(source);
                if (definition.Compatibility.Contains(currentCoreVersion.Current.Tag))
                    definitions[definition.Id] = definition;
            }
        }

        public DatabaseTableDefinitionJson? GetDefinition(string definitionId)
        {
            if (definitionId != null && definitions.TryGetValue(definitionId, out var definition))
                return definition;
            return null;
        }

        public IEnumerable<DatabaseTableDefinitionJson> Definitions => definitions.Values;
    }
}