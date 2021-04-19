using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Loaders
{
    [SingleInstance]
    [AutoRegister]
    public class DatabaseTableDataProvider : IDatabaseTableDataProvider
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly IMySqlExecutor sqlExecutor;
        private readonly IDatabaseTableModelGenerator _tableModelGenerator;
        
        public DatabaseTableDataProvider(ITableDefinitionProvider tableDefinitionProvider, IMySqlExecutor sqlExecutor, IDatabaseFieldFactory tableFieldFactory, 
            IDatabaseTableModelGenerator tableModelGenerator)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.sqlExecutor = sqlExecutor;
            this._tableModelGenerator = tableModelGenerator;
        }

        private string BuildSQLQueryFromTableDefinition(in DatabaseTableDefinitionJson tableDefinitionJson, uint entry)
        {
            var columns = tableDefinitionJson.Groups.SelectMany(x => x.Fields).Select(x => $"`{x.DbColumnName}`");
            var names = string.Join(",", columns);

            return
                $"SELECT {names} FROM {tableDefinitionJson.TableName} WHERE {tableDefinitionJson.TablePrimaryKeyColumnName} = {entry};";
        }

        public async Task<IDatabaseTableData?> Load(string table, uint key)
        {
            var definition = tableDefinitionProvider.GetDefinition(table);
            if (definition == null)
                return null;
            
            var sqlStatement = BuildSQLQueryFromTableDefinition(definition, key);
            var result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

            if (result == null || result.Count == 0)
                return null;

            if (definition.IsMultiRecord)
                return _tableModelGenerator.GetDatabaseMultiRecordTable(key, definition, result);

            return _tableModelGenerator.GetDatabaseTable(definition, result[0]);
        }
    }
}