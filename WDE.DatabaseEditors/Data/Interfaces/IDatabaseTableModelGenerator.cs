using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces
{
    [UniqueProvider]
    public interface IDatabaseTableModelGenerator
    {
        IDatabaseTableData? CreateDatabaseTable(DatabaseTableDefinitionJson tableDefinition, uint[] keys,
            IList<Dictionary<string, (System.Type type, object value)>> fieldsFromDb);

        DatabaseEntity CreateEmptyEntity(DatabaseTableDefinitionJson tableDefinitionJson, uint key);
    }
}