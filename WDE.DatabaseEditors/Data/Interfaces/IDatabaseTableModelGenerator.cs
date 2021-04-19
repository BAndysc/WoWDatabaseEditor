using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces
{
    [UniqueProvider]
    public interface IDatabaseTableModelGenerator
    {
        IDatabaseTableData? GetDatabaseTable(in DatabaseTableDefinitionJson tableDefinition, 
            Dictionary<string, object> fieldsFromDb);

        IDatabaseTableData? GetDatabaseMultiRecordTable(uint key, in DatabaseTableDefinitionJson tableDefinition, 
            IList<Dictionary<string, object>> records);
    }
}