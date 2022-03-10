using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseTableData : IDatabaseTableData
    {
        public DatabaseTableDefinitionJson TableDefinition { get; }
        public IReadOnlyList<DatabaseEntity> Entities { get; }

        public DatabaseTableData(DatabaseTableDefinitionJson definitionJson, IReadOnlyList<DatabaseEntity> entities)
        {
            TableDefinition = definitionJson;
            Entities = entities;
        }
    }
}