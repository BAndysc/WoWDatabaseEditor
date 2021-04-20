using System.Collections.Generic;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseTableData
    {
        DatabaseTableDefinitionJson TableDefinition { get; }
        List<DatabaseEntity> Entities { get; }
    }
}