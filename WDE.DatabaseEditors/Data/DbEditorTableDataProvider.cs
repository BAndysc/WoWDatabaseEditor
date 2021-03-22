using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class DbEditorTableDataProvider : IDbEditorTableDataProvider
    {
        private readonly IDbTableDefinitionProvider tableDefinitionProvider;
        private readonly IMySqlExecutor sqlExecutor;
        private readonly IDbTableDataProvider tableDataProvider;
        
        public DbEditorTableDataProvider(IDbTableDefinitionProvider tableDefinitionProvider, IMySqlExecutor sqlExecutor, IDbTableFieldFactory tableFieldFactory, 
            IDbTableDataProvider tableDataProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.sqlExecutor = sqlExecutor;
            this.tableDataProvider = tableDataProvider;
        }

        public async Task<IDbTableData?> LoadCreatureTemplateDataEntry(uint creatureEntry)
        {
            var tableDefinition = tableDefinitionProvider.GetCreatureTemplateDefinition();
            var sqlStatement = BuildSQLQueryFromTableDefinition(in tableDefinition, creatureEntry);
            var result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

            if (result == null || result.Count == 0)
                return null;

            return tableDataProvider.GetDatabaseTable(in tableDefinition, result[0]);
        }

        public async Task<IDbTableData?> LoadGameobjectTemplateDataEntry(uint goEntry)
        {
            var tableDefinition = tableDefinitionProvider.GetGameobjectTemplateDefinition();
            var sqlStatement = BuildSQLQueryFromTableDefinition(in tableDefinition, goEntry);
            var result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

            if (result == null || result.Count == 0)
                return null;

            return tableDataProvider.GetDatabaseTable(in tableDefinition, result[0]);
        }

        public async Task<IDbTableData?> LoadCreatureLootTemplateData(uint entry)
        {
            var tableDefinition = tableDefinitionProvider.GetCreatureLootTemplateDefinition();
            var sqlStatement = BuildSQLQueryFromTableDefinition(in tableDefinition, entry);
            var result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

            if (result == null || result.Count == 0)
                return null;

            return tableDataProvider.GetDatabaseMultiRecordTable(in tableDefinition, result);
        }

        private string BuildSQLQueryFromTableDefinition(in DatabaseEditorTableDefinitionJson tableDefinitionJson, uint entry)
        {
            var columns = tableDefinitionJson.Groups.SelectMany(x => x.Fields).Select(x => $"`{x.DbColumnName}`");
            var names = string.Join(",", columns);

            return
                $"SELECT {names} FROM {tableDefinitionJson.TableName} WHERE {tableDefinitionJson.TablePrimaryKeyColumnName} = {entry};";
        }
    }
}