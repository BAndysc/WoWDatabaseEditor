using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Utils;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Loaders
{
    [SingleInstance]
    [AutoRegister]
    public class DatabaseTableDataProvider : IDatabaseTableDataProvider
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly IMySqlExecutor sqlExecutor;
        private readonly IDatabaseTableModelGenerator tableModelGenerator;
        
        public DatabaseTableDataProvider(ITableDefinitionProvider tableDefinitionProvider, IMySqlExecutor sqlExecutor, IDatabaseFieldFactory tableFieldFactory, 
            IDatabaseTableModelGenerator tableModelGenerator)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.sqlExecutor = sqlExecutor;
            this.tableModelGenerator = tableModelGenerator;
        }

        private string BuildSQLQueryFromTableDefinition(in DatabaseTableDefinitionJson tableDefinitionJson, uint[] entries)
        {
            var columns = tableDefinitionJson.Groups.SelectMany(x => x.Fields).Select(x => $"`{x.DbColumnName}`").Distinct();
            var names = string.Join(",", columns);

            return
                $"SELECT {names} FROM {tableDefinitionJson.TableName} WHERE {tableDefinitionJson.TablePrimaryKeyColumnName} IN ({string.Join(", ", entries)});";
        }

        public async Task<IDatabaseTableData?> Load(string table, params uint[] keys)
        {
            var definition = tableDefinitionProvider.GetDefinition(table);
            if (definition == null)
                return null;
            
            var sqlStatement = BuildSQLQueryFromTableDefinition(definition, keys);
            var result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

            if (result == null || result.Count == 0)
                result = BuildEmptyEntities(definition, keys).ToList();

            //if (definition.IsMultiRecord)
            //    return tableModelGenerator.GetDatabaseMultiRecordTable(key, definition, result);

            return tableModelGenerator.GetDatabaseTable(definition, result);
        }

        private IEnumerable<Dictionary<string, (Type, object)>> BuildEmptyEntities(DatabaseTableDefinitionJson definition, uint[] keys)
        {
            foreach (var key in keys)
            {
                Dictionary<string, (Type type, object value)> entity = new();
                foreach (var column in definition.Groups.SelectMany(t => t.Fields)
                    .Distinct(
                        EqualityComparerFactory.Create<DbEditorTableGroupFieldJson>(
                            f => f.DbColumnName.GetHashCode(),
                            (a, b) => a.DbColumnName.Equals(b.DbColumnName))))
                {
                    Type type = typeof(string);
                    object value = "";
                    if (column.ValueType == "float")
                    {
                        type = typeof(float);
                        value = 0.0f;
                    }
                    else if (column.ValueType.EndsWith("Parameter") || column.ValueType == "int" ||
                             column.ValueType == "uint")
                    {
                        type = typeof(int);
                        value = 0;
                    }

                    if (column.DbColumnName == definition.TablePrimaryKeyColumnName)
                    {
                        type = typeof(int);
                        value = (int)key;
                    }

                    entity[column.DbColumnName] = (type, value);
                }

                yield return entity;
            }
        }
    }
}