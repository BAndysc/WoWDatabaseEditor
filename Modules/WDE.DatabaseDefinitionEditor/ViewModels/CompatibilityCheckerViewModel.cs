using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Services;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseDefinitionEditor.ViewModels
{
    [AutoRegister]
    public partial class CompatibilityCheckerViewModel : BindableBase
    {
        private readonly IDatabaseQueryExecutor sqlExecutor;
        private List<DatabaseTableDefinitionJson> allDefinitions = new();
        public ObservableCollection<DatabaseTableDefinitionJson> Definitions { get; } = new();

        public INativeTextDocument Raport { get; }

        [Notify] private string searchText = "";

        private DatabaseTableDefinitionJson? selectedDefinition;
        public DatabaseTableDefinitionJson? SelectedDefinition
        {
            get => selectedDefinition;
            set
            {
                SetProperty(ref selectedDefinition, value);
                if (value != null)
                    Evaluate(value).ListenErrors();
            }
        }

        private async Task Evaluate(DatabaseTableDefinitionJson value)
        {
            StringBuilder raport = new();

            raport.AppendLine($" === {value.Id} compatibility raport ===");
            
            var columnsByTables = value.Groups.SelectMany(g => g.Fields)
                .Where(g => g.IsActualDatabaseColumn)
                .GroupBy(g => g.ForeignTable ?? value.TableName)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var table in columnsByTables)
            {
                var databaseTable = new DatabaseTable(value.DataDatabaseType, table.Key);
                if (!await DatabaseContainsTable(databaseTable))
                {
                    raport.AppendLine($" [ ERROR ] Table {databaseTable} doesn't exist! Cannot check compatibility");
                    foreach (var column in table.Value)
                        raport.AppendLine($"    [ ERROR ] Therefore column {table.Key}.{column.Name} doesn't exist");
                    raport.AppendLine();
                    continue;
                }
                
                raport.AppendLine($" [  OK   ] Table {table.Key} exist");

                var tableColumns = await sqlExecutor.GetTableColumns(value.DataDatabaseType, table.Key);
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
                        if (dbDefinition.PrimaryKey && !(value.PrimaryKey?.Contains(column.DbColumnFullName) ?? false))
                            raport.AppendLine($" [ WARN  ] Column {table.Key}.{column.DbColumnName} should be marked as primary key!");
                        else if (!dbDefinition.PrimaryKey && (value.PrimaryKey?.Contains(column.DbColumnFullName) ?? false))
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

        private HashSet<DatabaseTable>? cachedTables;
        
        private async Task<bool> DatabaseContainsTable(DatabaseTable table)
        {
            if (cachedTables == null)
            {
                var world = await sqlExecutor.GetTables(DataDatabaseType.World);
                var hotfix = await sqlExecutor.GetTables(DataDatabaseType.Hotfix);
                cachedTables = new HashSet<DatabaseTable>(world.Concat(hotfix));
            }

            return cachedTables.Contains(table);
        }

        public CompatibilityCheckerViewModel(ITableDefinitionProvider definitionProvider,
            IDatabaseQueryExecutor sqlExecutor,
            INativeTextDocument document)
        {
            this.sqlExecutor = sqlExecutor;
            allDefinitions.AddRange(definitionProvider.AllDefinitions);
            Raport = document;
            this.ToObservable<string, CompatibilityCheckerViewModel>(o => o.SearchText)
                .SubscribeAction(_ => DoSearch());
        }

        private void DoSearch()
        {
            Definitions.Clear();
            if (string.IsNullOrWhiteSpace(searchText))
                Definitions.AddRange(allDefinitions);
            else
            {
                foreach (var def in allDefinitions)
                {
                    if (def.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
                        Definitions.Add(def);
                }
            }
        }
    }
}