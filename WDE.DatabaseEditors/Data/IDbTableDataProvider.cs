using System.Collections.Generic;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [UniqueProvider]
    public interface IDbTableDataProvider
    {
        IDbTableData? GetDatabaseTable(in DatabaseEditorTableDefinitionJson tableDefinition, 
            Dictionary<string, object> fieldsFromDb);

        IDbTableData? GetDatabaseMultiRecordTable(in DatabaseEditorTableDefinitionJson tableDefinition, 
            IList<Dictionary<string, object>> records);
    }
}