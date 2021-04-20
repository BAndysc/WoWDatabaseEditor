using System;
using System.Threading.Tasks;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemSqlProvider : ISolutionItemSqlProvider<DatabaseTableSolutionItem>
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IQueryGenerator queryGenerator;

        public DatabaseTableSolutionItemSqlProvider(ITableDefinitionProvider tableDefinitionProvider,
            IDatabaseTableDataProvider tableDataProvider, IQueryGenerator queryGenerator)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.tableDataProvider = tableDataProvider;
            this.queryGenerator = queryGenerator;
        }

        public async Task<string> GenerateSql(DatabaseTableSolutionItem item)
        {
            IDatabaseTableData? tableData = await LoadTable(item);

            return queryGenerator.GenerateQuery(tableData, item.TableId, item.Entries[0], item.ModifiedFields);
        }

        private Task<IDatabaseTableData?> LoadTable(DatabaseTableSolutionItem item)
        {
            return tableDataProvider.Load(item.TableId, item.Entries[0]);
        }
    }
}