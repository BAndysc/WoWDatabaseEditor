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
        private readonly List<DatabaseTableDefinitionJson> incompatibleDefinitionList = new();
        private readonly Dictionary<string, DatabaseTableDefinitionJson> incompatibleDefinitions = new();
        private readonly Dictionary<string, DatabaseTableDefinitionJson> definitions = new();
        private readonly Dictionary<string, DatabaseTableDefinitionJson> definitionsByTableName = new();
        private readonly Dictionary<string, DatabaseTableDefinitionJson> definitionsByForeignTableName = new();

        public TableDefinitionProvider(ITableDefinitionDeserializer serializationProvider,
            ITableDefinitionJsonProvider jsonProvider,
            ICurrentCoreVersion currentCoreVersion)
        {
            Dictionary<string, List<DatabaseTableReferenceJson>> fileToExtraCompatibility = new();
            foreach (var source in jsonProvider.GetDefinitionReferences())
            {
                var reference =
                    serializationProvider.DeserializeTableDefinition<DatabaseTableReferenceJson>(source.content);
                if (!fileToExtraCompatibility.ContainsKey(reference.File))
                    fileToExtraCompatibility[reference.File] = new List<DatabaseTableReferenceJson>();
                fileToExtraCompatibility[reference.File].Add(reference);
            }
            
            foreach (var source in jsonProvider.GetDefinitionSources())
            {
                var definition =
                    serializationProvider.DeserializeTableDefinition<DatabaseTableDefinitionJson>(source.content);

                if (string.IsNullOrEmpty(definition.MultiSolutionName))
                    definition.MultiSolutionName = definition.Name;

                if (string.IsNullOrEmpty(definition.SingleSolutionName))
                    definition.SingleSolutionName = definition.Name;

                if (string.IsNullOrEmpty(definition.IconPath))
                    definition.IconPath = "Icons/document_table.png";
                
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

                if (definition.Compatibility.Contains(currentCoreVersion.Current.Tag) ||
                    fileToExtraCompatibility.TryGetValue(source.file, out var reference) &&
                    reference.Any(r => r.Compatibility == currentCoreVersion.Current.Tag))
                {
                    definitions[definition.Id] = definition;
                    definitionsByTableName[definition.TableName] = definition;
                    if (definition.ForeignTable != null)
                    {
                        foreach (var foreign in definition.ForeignTable)
                            definitionsByForeignTableName[foreign.TableName] = definition;
                    }
                }
                else
                {
                    incompatibleDefinitions[definition.Id] = definition;
                    incompatibleDefinitionList.Add(definition);
                }
            }
        }

        public IEnumerable<string>? CoreCompatibility(string definitionId)
        {
            if (incompatibleDefinitions.TryGetValue(definitionId, out var definition))
                return definition.Compatibility;
            return null;
        }

        public DatabaseTableDefinitionJson? GetDefinitionByTableName(string? tableName)
        {
            if (tableName == null)
                return null;
            
            if (definitionsByTableName.TryGetValue(tableName, out var definition))
                return definition;
            
            return null;
        }
        
        public DatabaseTableDefinitionJson? GetDefinitionByForeignTableName(string? tableName)
        {
            if (tableName == null)
                return null;
            
            if (definitionsByForeignTableName.TryGetValue(tableName, out var definition))
                return definition;
            
            return null;
        }

        public DatabaseTableDefinitionJson? GetDefinition(string? definitionId)
        {
            if (definitionId != null && definitions.TryGetValue(definitionId, out var definition))
                return definition;
            return null;
        }

        public IEnumerable<DatabaseTableDefinitionJson> IncompatibleDefinitions => incompatibleDefinitions.Values;
        public IEnumerable<DatabaseTableDefinitionJson> AllDefinitions =>
            definitions.Values.Concat(incompatibleDefinitionList);
        public IEnumerable<DatabaseTableDefinitionJson> Definitions => definitions.Values;
    }
}