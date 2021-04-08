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
            IDbTableData? tableData = null;
            
            if (item.IsMultiRecord)
                tableData = await LoadTable(item.TableContentType, item.Entry);

            return queryGenerator.GenerateQuery(tableData, item.TableContentType, item.Entry, item.IsMultiRecord, item.ModifiedFields);
        }

        private Task<IDbTableData?> LoadTable(DbTableContentType tableContentType, uint key)
        {
            switch (tableContentType)
            {
                case DbTableContentType.CreatureLootTemplate:
                    return tableDataProvider.LoadCreatureLootTemplateData(key);
                default:
                    throw new Exception("[DbEditorsSolutionItemSqlProvider] not defined table content type!");
            }
        }
    }
}