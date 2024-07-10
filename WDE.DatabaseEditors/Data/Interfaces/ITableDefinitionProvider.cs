using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces
{
    [UniqueProvider]
    public interface ITableDefinitionProvider
    {
        DatabaseTableDefinitionJson? GetDefinitionByTableName(DatabaseTable? tableName);
        DatabaseTableDefinitionJson? GetDefinition(DatabaseTable? tableName) => GetDefinitionByTableName(tableName);
        DatabaseTableDefinitionJson? GetDefinitionByForeignTableName(DatabaseTable? tableName);
        IEnumerable<string>? CoreCompatibility(DatabaseTable definitionId);
        IEnumerable<DatabaseTableDefinitionJson> Definitions { get; }
        IEnumerable<DatabaseTableDefinitionJson> IncompatibleDefinitions { get; }
        
        IEnumerable<DatabaseTableDefinitionJson> AllDefinitions { get; }
        
        event Action? DefinitionsChanged;
    }
}