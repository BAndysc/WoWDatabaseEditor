using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces
{
    [UniqueProvider]
    public interface ITableDefinitionProvider
    {
        DatabaseTableDefinitionJson? GetDefinition(string tableName);
        IEnumerable<DatabaseTableDefinitionJson> Definitions { get; }
    }
}