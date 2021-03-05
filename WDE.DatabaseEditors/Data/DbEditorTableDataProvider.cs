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

        public async Task<IDbTableData> LoadCreatureTamplateDataEntry(int creatureEntry)
        {
            var tableDefinition = tableDefinitionProvider.GetCreatureTemplateDefinition();
            var sqlStatement = BuildSQLQueryFromTableDefinition(in tableDefinition, creatureEntry);
            var result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

            if (result == null || result.Count == 0)
                return null;

            return tableDataProvider.GetDatabaseTable(in tableDefinition, tableFieldFactory, result[0]);
        }

        private string BuildSQLQueryFromTableDefinition(in DatabaseEditorTableDefinitionJson tableDefinitionJson, int creatureEntry)
        {
            // StringBuilder stringBuilder = new StringBuilder("SELECT ");

            var columns = tableDefinitionJson.Groups.SelectMany(x => x.Fields).Select(x => $"`{x.DbColumnName}`");
            var names = string.Join(",", columns);
                
            // foreach (var group in tableDefinitionJson.Groups)
                // group.Fields
            // foreach (var field in group.Fields)
                // stringBuilder.Append($"{field.DbColumnName}, ");

            // stringBuilder.Remove(stringBuilder.Length - 3, 2);
            // stringBuilder.Append($" FROM {tableDefinitionJson.TableName} WHERE {tableDefinitionJson.TableIndexFieldName} = {creatureEntry}");
            // return stringBuilder.ToString();
            return
                $"SELECT {names} FROM {tableDefinitionJson.TableName} WHERE {tableDefinitionJson.TableIndexFieldName} = {creatureEntry};";
        }
    }
}