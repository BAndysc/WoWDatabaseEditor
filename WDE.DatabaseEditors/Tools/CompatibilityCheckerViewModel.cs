using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Tools
{
#if DEBUGAVALONIA
    [AutoRegister]
    public class CompatibilityCheckerViewModel : BindableBase
    {
        private readonly IMySqlExecutor sqlExecutor;
        public ObservableCollection<DatabaseTableDefinitionJson> Definitions { get; } = new();

        public INativeTextDocument Raport { get; }

        private DatabaseTableDefinitionJson? selectedDefinition;
        public DatabaseTableDefinitionJson? SelectedDefinition
        {
            get => selectedDefinition;
            set
            {
                SetProperty(ref selectedDefinition, value);
                if (value != null)
                    Evaluate(value);
            }
        }

        private async Task Evaluate(DatabaseTableDefinitionJson value)
        {
            StringBuilder raport = new();

            raport.AppendLine($" === {value.Id} compatibility raport ===");
            
            var columnsByTables = value.Groups.SelectMany(g => g.Fields)
                .GroupBy(g => g.ForeignTable ?? value.TableName)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var table in columnsByTables)
            {
                if (!await DatabaseContainsTable(table.Key))
                {
                    raport.AppendLine($" [ ERROR ] Table {table.Key} doesn't exist! Cannot check compatibility");
                    foreach (var column in table.Value)
                        raport.AppendLine($"    [ ERROR ] Therefore column {table.Key}.{column.Name} doesn't exist");
                    raport.AppendLine();
                    continue;
                }
                
                raport.AppendLine($" [  OK   ] Table {table.Key} exist");

                var tableColumns = await sqlExecutor.GetTableColumns(table.Key);
                var columnsByName = tableColumns
                    .GroupBy(g => g.ColumnName)
                    .ToDictionary(g => g.Key, g => g.FirstOrDefault());
                
                foreach (var column in table.Value)
                {
                    if (!columnsByName.TryGetValue(column.DbColumnName, out var dbDefinition))
                    {
                        raport.AppendLine($" [ ERROR ] Column {table.Key}.{column.DbColumnName} doesn't exist!");
                        continue;
                    }

                    if (dbDefinition.Nullable && !column.CanBeNull)
                        raport.AppendLine($" [ WARN  ] Column {table.Key}.{column.DbColumnName} should be marked as nullable!");
                    else if (!dbDefinition.Nullable && column.CanBeNull)
                        raport.AppendLine($" [ WARN  ] Column {table.Key}.{column.DbColumnName} should be marked as NON nullable");

                    if (column.ForeignTable == null)
                    {
                        if (dbDefinition.PrimaryKey && !(value.PrimaryKey?.Contains(column.DbColumnName) ?? false))
                            raport.AppendLine($" [ WARN  ] Column {table.Key}.{column.DbColumnName} should be marked as primary key!");
                        else if (!dbDefinition.PrimaryKey && (value.PrimaryKey?.Contains(column.DbColumnName) ?? false))
                            raport.AppendLine($" [ WARN  ] Column {table.Key}.{column.DbColumnName} marked as primary key, but is not!");   
                    }
                }

                foreach (var column in columnsByName.Keys)
                {
                    if (table.Value.Find(t => t.DbColumnName == column) == null)
                        raport.AppendLine($" [ ERROR ] Column {table.Key}.{column} not found in definition!");
                }
                
                raport.AppendLine();
            }

            Raport.FromString(raport.ToString());
        }

        private HashSet<string>? cachedTables;
        
        private async Task<bool> DatabaseContainsTable(string table)
        {
            cachedTables ??= (await sqlExecutor.GetTables()).ToHashSet();

            return cachedTables.Contains(table);
        }

        public CompatibilityCheckerViewModel(ITableDefinitionProvider definitionProvider,
            IMySqlExecutor sqlExecutor,
            INativeTextDocument document)
        {
            this.sqlExecutor = sqlExecutor;
            Definitions.AddRange(definitionProvider.AllDefinitions);
            Raport = document;
        }
    }
#endif
}