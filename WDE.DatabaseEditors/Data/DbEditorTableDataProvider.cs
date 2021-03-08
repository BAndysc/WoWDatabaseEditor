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
        private readonly IDbTableFieldFactory tableFieldFactory;
        private readonly IDbTableDataProvider tableDataProvider;
        
        public DbEditorTableDataProvider(IDbTableDefinitionProvider tableDefinitionProvider, IMySqlExecutor sqlExecutor, IDbTableFieldFactory tableFieldFactory, 
            IDbTableDataProvider tableDataProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.sqlExecutor = sqlExecutor;
            this.tableFieldFactory = tableFieldFactory;
            this.tableDataProvider = tableDataProvider;
        }

        public async Task<IDbTableData> LoadCreatureTamplateDataEntry(uint creatureEntry)
        {
            var tableDefinition = tableDefinitionProvider.GetCreatureTemplateDefinition();
            var sqlStatement = BuildSQLQueryFromTableDefinition(in tableDefinition, creatureEntry);
            var result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

            if (result == null || result.Count == 0)
                return null;

            return tableDataProvider.GetDatabaseTable(in tableDefinition, tableFieldFactory, result[0]);
        }

        public async Task<IDbTableData> LoadGameobjectTamplateDataEntry(uint goEntry)
        {
            var tableDefinition = tableDefinitionProvider.GetGameobjectTemplateDefinition();
            var sqlStatement = BuildSQLQueryFromTableDefinition(in tableDefinition, goEntry);
            var result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

            if (result == null || result.Count == 0)
                return null;

            return tableDataProvider.GetDatabaseTable(in tableDefinition, tableFieldFactory, result[0]);
        }

        private string BuildSQLQueryFromTableDefinition(in DatabaseEditorTableDefinitionJson tableDefinitionJson, uint creatureEntry)
        {
            var columns = tableDefinitionJson.Groups.SelectMany(x => x.Fields).Select(x => $"`{x.DbColumnName}`");
            var names = string.Join(",", columns);

            return
                $"SELECT {names} FROM {tableDefinitionJson.TableName} WHERE {tableDefinitionJson.TableIndexFieldName} = {creatureEntry};";
        }
    }
}