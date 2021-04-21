using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
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
            // missing table data we have to load it
            //if (isMultiRecord)
            //{
            //    if (tableData is DbMultiRecordTableData multiRecordTableData)
            //        return MultiRecordTableSqlGenerator.GenerateSql(multiRecordTableData);
            //}
            
            return GenerateUpdateQuery(tableData);
        }

        private string GenerateUpdateQuery(IDatabaseTableData tableData)
        {
            StringBuilder query = new();
            
            foreach (var entity in tableData.Entities)
            {
                var keyColumn = entity.GetCell(tableData.TableDefinition.TablePrimaryKeyColumnName);

                if (keyColumn == null)
                    throw new Exception("Cannot generate update query from entity that doesn't have key column");
                
                var updates = string.Join(", ",
                    entity.Fields
                        .Where(f => f.IsModified)
                        .Select(f => $"`{f.FieldName}` = {f.ToQueryString()}"));
                
                if (string.IsNullOrEmpty(updates))
                    continue;
                
                var updateQuery = $"UPDATE `{tableData.TableDefinition.TableName}` SET {updates} WHERE `{tableData.TableDefinition.TablePrimaryKeyColumnName}`= {keyColumn.ToQueryString()}";
                query.AppendLine(updateQuery);
            }

            return query.ToString();
        }

        private string BuildUpdateQuery(Dictionary<string, DatabaseSolutionItemModifiedField> fields, string tableName, 
            string keyColumnName, uint itemKey)
        {
            var updateParts = fields.Select(p => BuildFieldUpdateExpression(p.Value));
            string fieldsString = string.Join(", ", updateParts);
            return $"UPDATE `{tableName}` SET {fieldsString} WHERE `{keyColumnName}`= {itemKey};";
        }

        private string BuildFieldUpdateExpression(DatabaseSolutionItemModifiedField modifiedField)
        {
            var paramValue = modifiedField.NewValue is string ? $"\"{modifiedField.NewValue}\"" : 
                (modifiedField.NewValue is null ? "NULL" : $"{modifiedField.NewValue}");
            return $"`{modifiedField.DbFieldName}`={paramValue}";
        }

        private string BuildInsertWithDeleteQuery(Dictionary<string, DatabaseSolutionItemModifiedField> fields, string tableName, 
            string keyColumnName, uint itemKey)
        {
            return $"DELETE FROM `{tableName}` WHERE `{keyColumnName}`= {itemKey};";
        }
    }
    
    public interface IQueryGenerator
    {
        public string GenerateQuery(IDatabaseTableData tableData);
    }
}