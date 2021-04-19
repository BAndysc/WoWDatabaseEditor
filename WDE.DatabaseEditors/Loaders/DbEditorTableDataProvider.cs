using System.Linq;
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

        private string BuildSQLQueryFromTableDefinition(in DatabaseEditorTableDefinitionJson tableDefinitionJson, uint entry)
        {
            var columns = tableDefinitionJson.Groups.SelectMany(x => x.Fields).Select(x => $"`{x.DbColumnName}`");
            var names = string.Join(",", columns);

            return
                $"SELECT {names} FROM {tableDefinitionJson.TableName} WHERE {tableDefinitionJson.TablePrimaryKeyColumnName} = {entry};";
        }

        public async Task<IDbTableData?> Load(string table, uint key)
        {
            var definition = tableDefinitionProvider.GetDefinition(table);
            if (definition == null)
                return null;
            
            var sqlStatement = BuildSQLQueryFromTableDefinition(definition, key);
            var result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

            if (result == null || result.Count == 0)
                return null;

            if (definition.IsMultiRecord)
                return tableDataProvider.GetDatabaseMultiRecordTable(key, definition, result);

            return tableDataProvider.GetDatabaseTable(definition, result[0]);
        }
    }
}