using System;
using System.Threading.Tasks;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DbEditorsSolutionItemSqlProvider : ISolutionItemSqlProvider<DbEditorsSolutionItem>
    {
        private readonly IDbTableDefinitionProvider tableDefinitionProvider;
        private readonly IDbEditorTableDataProvider tableDataProvider;
        private readonly IQueryGenerator queryGenerator;

        public DbEditorsSolutionItemSqlProvider(IDbTableDefinitionProvider tableDefinitionProvider,
            IDbEditorTableDataProvider tableDataProvider, IQueryGenerator queryGenerator)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.tableDataProvider = tableDataProvider;
            this.queryGenerator = queryGenerator;
        }

        public async Task<string> GenerateSql(DbEditorsSolutionItem item)
        {
            IDbTableData? tableData = await LoadTable(item);

            return queryGenerator.GenerateQuery(tableData, item.TableId, item.Entry, item.ModifiedFields);
        }

        private Task<IDbTableData?> LoadTable(DbEditorsSolutionItem item)
        {
            return tableDataProvider.Load(item.TableId, item.Entry);
        }
    }
}