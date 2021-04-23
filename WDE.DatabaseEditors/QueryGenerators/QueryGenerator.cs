using System.Collections.Generic;
using System.Linq;
using System.Text;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.QueryGenerators
{
    [SingleInstance]
    [AutoRegister]
    public class QueryGenerator : IQueryGenerator
    {       
        private readonly ITableDefinitionProvider tableDefinitionProvider;

        public QueryGenerator(ITableDefinitionProvider tableDefinitionProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
        }
        
        public string GenerateQuery(IDatabaseTableData tableData)
        {
            return GenerateUpdateQuery(tableData);
        }

        public string GenerateUpdateFieldQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity,
            IDatabaseField field)
        {
            var column = table.TableColumns[field.FieldName];
            string primaryKeyColumn = table.TablePrimaryKeyColumnName;
            if (column.ForeignTable != null)
                primaryKeyColumn = table.ForeignTableByName[column.ForeignTable].ForeignKey;
            
            return
                $"UPDATE {table.TableName} SET `{field.FieldName}` = {field.ToQueryString()} WHERE `{primaryKeyColumn}` = {entity.Key};";
        }
        
        private string GenerateUpdateQuery(IDatabaseTableData tableData)
        {
            StringBuilder query = new();
            
            foreach (var entity in tableData.Entities)
            {
                Dictionary<string, List<IDatabaseField>> fieldsByTable = null!;
                if (tableData.TableDefinition.ForeignTable != null)
                {
                    fieldsByTable = entity.Fields
                        .GroupBy(f => tableData.TableDefinition.TableColumns[f.FieldName].ForeignTable ?? tableData.TableDefinition.TableName)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var foreign in tableData.TableDefinition.ForeignTable)
                        fieldsByTable[foreign.TableName].Insert(0, new DatabaseField<long>(foreign.ForeignKey, new ValueHolder<long>(entity.Key)));
                }
                else
                {
                    fieldsByTable = new();
                    fieldsByTable[tableData.TableDefinition.TableName] = entity.Fields.ToList();
                }
                
                if (entity.ExistInDatabase)
                {
                    foreach (var table in fieldsByTable)
                    {
                        var updates = string.Join(", ",
                            table.Value
                                .Where(f => f.IsModified)
                                .Select(f => $"`{f.FieldName}` = {f.ToQueryString()}"));
                
                        if (string.IsNullOrEmpty(updates))
                            continue;

                        string primaryKeyColumn = tableData.TableDefinition.TablePrimaryKeyColumnName;
                        if (table.Key != tableData.TableDefinition.TableName)
                        {
                            primaryKeyColumn = tableData.TableDefinition.ForeignTableByName[table.Key].ForeignKey;
                            query.AppendLine(
                                $"INSERT IGNORE INTO {table.Key} (`{primaryKeyColumn}`) VALUES ({entity.Key});");
                        }
                        
                        var updateQuery = $"UPDATE `{table.Key}` SET {updates} WHERE `{primaryKeyColumn}`= {entity.Key};";
                        query.AppendLine(updateQuery);
                    }
                }
                else
                {
                    foreach (var table in fieldsByTable)
                    {
                        string primaryKeyColumn = tableData.TableDefinition.TablePrimaryKeyColumnName;
                        if (table.Key != tableData.TableDefinition.TableName)
                            primaryKeyColumn = tableData.TableDefinition.ForeignTableByName[table.Key].ForeignKey;
                        
                        query.AppendLine(
                            $"DELETE FROM {table.Key} WHERE `{primaryKeyColumn}` = {entity.Key};");
                        var columns = string.Join(", ", table.Value.Select(f => $"`{f.FieldName}`"));
                        query.AppendLine($"INSERT INTO {table.Key} ({columns}) VALUES");
                        var values = string.Join(", ", table.Value.Select(f => f.ToQueryString()));
                        query.AppendLine($"({values});");   
                    }
                }
            }

            return query.ToString();
        }
    }
    
    public interface IQueryGenerator
    {
        public string GenerateQuery(IDatabaseTableData tableData);
        public string GenerateUpdateFieldQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity, IDatabaseField field);
    }
}