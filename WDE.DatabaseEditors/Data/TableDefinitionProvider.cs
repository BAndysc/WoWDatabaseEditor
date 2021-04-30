using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<string, DatabaseTableDefinitionJson> incompatibleDefinitions = new();
        private readonly Dictionary<string, DatabaseTableDefinitionJson> definitions = new();
        
        public TableDefinitionProvider(ITableDefinitionDeserializer serializationProvider,
            ITableDefinitionJsonProvider jsonProvider,
            ICurrentCoreVersion currentCoreVersion)
        {
            foreach (var source in jsonProvider.GetDefinitionSources())
            {
                var definition =
                    serializationProvider.DeserializeTableDefinition<DatabaseTableDefinitionJson>(source.content);

                definition.TableColumns = new Dictionary<string, DatabaseColumnJson>();
                foreach (var group in definition.Groups)
                {
                    foreach (var column in group.Fields)
                    {
                        definition.TableColumns[column.DbColumnName] = column;
                    }
                }

                definition.FileName = source.file;
                
                if (definition.ForeignTable != null)
                {
                    definition.ForeignTableByName = new Dictionary<string, DatabaseForeignTableJson>();
                    foreach (var foreign in definition.ForeignTable)
                    {
                        definition.ForeignTableByName[foreign.TableName] = foreign;
                    }
                }

                if (definition.Compatibility.Contains(currentCoreVersion.Current.Tag))
                    definitions[definition.Id] = definition;
                else
                    incompatibleDefinitions[definition.Id] = definition;
            }
        }

        public IEnumerable<string>? CoreCompatibility(string definitionId)
        {
            if (incompatibleDefinitions.TryGetValue(definitionId, out var definition))
                return definition.Compatibility;
            return null;
        }
        
        public DatabaseTableDefinitionJson? GetDefinition(string definitionId)
        {
            if (definitionId != null && definitions.TryGetValue(definitionId, out var definition))
                return definition;
            return null;
        }

        public IEnumerable<DatabaseTableDefinitionJson> AllDefinitions =>
            definitions.Values.Concat(incompatibleDefinitions.Values);
        public IEnumerable<DatabaseTableDefinitionJson> Definitions => definitions.Values;
    }
}