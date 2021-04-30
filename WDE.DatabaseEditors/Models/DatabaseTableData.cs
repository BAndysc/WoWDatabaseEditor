using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseTableData : IDatabaseTableData
    {
        public DatabaseTableDefinitionJson TableDefinition { get; }
        public IList<DatabaseEntity> Entities { get; }

        public DatabaseTableData(DatabaseTableDefinitionJson definitionJson, IList<DatabaseEntity> entities)
        {
            TableDefinition = definitionJson;
            Entities = entities;
        }
    }
}