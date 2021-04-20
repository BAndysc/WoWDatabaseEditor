using System.Collections.Generic;
using System.ComponentModel;
using WDE.Common.History;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseTableData : IDatabaseTableData
    {
        public DatabaseTableDefinitionJson TableDefinition { get; }
        public List<DatabaseEntity> Entities { get; }

        public DatabaseTableData(DatabaseTableDefinitionJson definitionJson, List<DatabaseEntity> entities)
        {
            TableDefinition = definitionJson;
            Entities = entities;
        }
    }

    public class DatabaseEntity
    {
        public DatabaseEntity(Dictionary<string, IDatabaseField> cells)
        {
            Cells = cells;
            foreach (var databaseField in Cells)
            {
                databaseField.Value.OnChanged += action =>
                {
                    OnAction?.Invoke(action);
                };
            }
        }

        public event System.Action<IHistoryAction>? OnAction;

        public Dictionary<string, IDatabaseField> Cells { get; }

        public IDatabaseField? GetCell(string columnName)
        {
            if (Cells.TryGetValue(columnName, out var cell))
                return cell;
            return null;
        }
    }
}