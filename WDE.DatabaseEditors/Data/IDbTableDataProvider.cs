using System.Collections.Generic;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [UniqueProvider]
    public interface IDbTableDataProvider
    {
        IDbTableData GetDatabaseTable(in DatabaseEditorTableDefinitionJson tableDefinition, IDbTableFieldFactory fieldFactory, Dictionary<string, object> fieldsFromDb);
    }
}