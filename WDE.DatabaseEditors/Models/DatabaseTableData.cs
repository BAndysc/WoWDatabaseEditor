using System.Collections.Generic;
using System.ComponentModel;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseTableData : IDatabaseTableData
    {
        public DatabaseTableDefinitionJson TableDefinition { get; }
        public List<DatabaseEntity> Rows { get; }

        public DatabaseTableData(DatabaseTableDefinitionJson definitionJson, List<DatabaseEntity> rows)
        {
            TableDefinition = definitionJson;
            Rows = rows;
        }
    }

    public class DatabaseEntity
    {
        public DatabaseEntity(Dictionary<string, IDatabaseField> cells)
        {
            Cells = cells;
        }

        public Dictionary<string, IDatabaseField> Cells { get; }

        public IDatabaseField? GetCell(string columnName)
        {
            if (Cells.TryGetValue(columnName, out var cell))
                return cell;
            return null;
        }
    }
}