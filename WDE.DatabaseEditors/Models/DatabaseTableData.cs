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
        public Dictionary<string, IDatabaseField> Cells { get; }
        
        public IEnumerable<IDatabaseField> Fields => Cells.Values;

        public event System.Action<IHistoryAction>? OnAction;
        
        public bool ExistInDatabase { get; }
        
        public uint Key { get; }
        
        public DatabaseEntity(bool existInDatabase, uint key, Dictionary<string, IDatabaseField> cells)
        {
            ExistInDatabase = existInDatabase;
            Key = key;
            Cells = cells;
            foreach (var databaseField in Cells)
            {
                databaseField.Value.OnChanged += action =>
                {
                    OnAction?.Invoke(action);
                };
            }
        }

        public IDatabaseField? GetCell(string columnName)
        {
            if (Cells.TryGetValue(columnName, out var cell))
                return cell;
            return null;
        }
    }
}