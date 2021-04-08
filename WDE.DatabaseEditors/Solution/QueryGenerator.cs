using System;
using System.Collections.Generic;
using System.Linq;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [SingleInstance]
    [AutoRegister]
    public class QueryGenerator : IQueryGenerator
    {       
        private readonly IDbTableDefinitionProvider tableDefinitionProvider;

        public QueryGenerator(IDbTableDefinitionProvider tableDefinitionProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
        }
        
        public string GenerateQuery(IDbTableData? tableData, DbTableContentType contentType, uint entry, bool isMultiRecord,
            Dictionary<string, DbTableSolutionItemModifiedField> modifiedFields)
        {
            // missing table data we have to load it
            if (isMultiRecord)
            {
                if (tableData is DbMultiRecordTableData multiRecordTableData)
                    return MultiRecordTableSqlGenerator.GenerateSql(multiRecordTableData);
            }
            
            var tableDefinition = tableDefinitionProvider.GetDefinition(contentType);
            return GenerateUpdateQuery(modifiedFields, tableDefinition.TableName,
                tableDefinition.TablePrimaryKeyColumnName, entry, isMultiRecord);
        }

        private string GenerateUpdateQuery(Dictionary<string, DbTableSolutionItemModifiedField> fields, string tableName, 
            string keyColumnName, uint itemKey, bool buildInsert)
        {
            if (fields == null || fields.Count == 0)
                return "";

            return buildInsert ? BuildInsertWithDeleteQuery(fields, tableName, keyColumnName, itemKey) 
                : BuildUpdateQuery(fields, tableName, keyColumnName, itemKey);
        }

        private string BuildUpdateQuery(Dictionary<string, DbTableSolutionItemModifiedField> fields, string tableName, 
            string keyColumnName, uint itemKey)
        {
            var updateParts = fields.Select(p => BuildFieldUpdateExpression(p.Value));
            string fieldsString = string.Join(", ", updateParts);
            return $"UPDATE `{tableName}` SET {fieldsString} WHERE `{keyColumnName}`= {itemKey};";
        }

        private string BuildFieldUpdateExpression(DbTableSolutionItemModifiedField modifiedField)
        {
            var paramValue = modifiedField.NewValue is string ? $"\"{modifiedField.NewValue}\"" : 
                (modifiedField.NewValue is null ? "NULL" : $"{modifiedField.NewValue}");
            return $"`{modifiedField.DbFieldName}`={paramValue}";
        }

        private string BuildInsertWithDeleteQuery(Dictionary<string, DbTableSolutionItemModifiedField> fields, string tableName, 
            string keyColumnName, uint itemKey)
        {
            return $"DELETE FROM `{tableName}` WHERE `{keyColumnName}`= {itemKey};";
        }
    }
    
    public interface IQueryGenerator
    {
        public string GenerateQuery(IDbTableData? tableData, DbTableContentType contentType, uint entry, bool isMultiRecord,
            Dictionary<string, DbTableSolutionItemModifiedField> modifiedFields);
    }
}