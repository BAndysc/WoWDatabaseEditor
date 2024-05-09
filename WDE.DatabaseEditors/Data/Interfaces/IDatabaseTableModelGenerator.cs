using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces
{
    [UniqueProvider]
    public interface IDatabaseTableModelGenerator
    {
        IDatabaseTableData? CreateDatabaseTable(DatabaseTableDefinitionJson tableDefinition, DatabaseKey[]? keys,
            IDatabaseSelectResult fieldsFromDb,
            IReadOnlyList<ColumnFullName> selectedColumns, IList<IConditionLine>[]? conditionsPerRow);

        DatabaseEntity CreateEmptyEntity(DatabaseTableDefinitionJson tableDefinitionJson, DatabaseKey key, bool phantomEntity);
    }
}