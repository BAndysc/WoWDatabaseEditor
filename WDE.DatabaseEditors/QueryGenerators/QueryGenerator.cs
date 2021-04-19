using System.Collections.Generic;
using System.Linq;
using WDE.DatabaseEditors.Data.Interfaces;
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
        
        public string GenerateQuery(IDatabaseTableData? tableData, string tableName, uint entry,
            Dictionary<string, DatabaseSolutionItemModifiedField> modifiedFields)
        {
            var tableDefinition = tableDefinitionProvider.GetDefinition(tableName);
            if (tableDefinition == null)
                return "-- invalid table --";
            // missing table data we have to load it
            //if (isMultiRecord)
            //{
            //    if (tableData is DbMultiRecordTableData multiRecordTableData)
            //        return MultiRecordTableSqlGenerator.GenerateSql(multiRecordTableData);
            //}
            
            return GenerateUpdateQuery(modifiedFields, tableDefinition.TableName,
                tableDefinition.TablePrimaryKeyColumnName, entry, tableDefinition.IsMultiRecord);
        }

        private string GenerateUpdateQuery(Dictionary<string, DatabaseSolutionItemModifiedField> fields, string tableName, 
            string keyColumnName, uint itemKey, bool buildInsert)
        {
            if (fields == null || fields.Count == 0)
                return "";

            return buildInsert ? BuildInsertWithDeleteQuery(fields, tableName, keyColumnName, itemKey) 
                : BuildUpdateQuery(fields, tableName, keyColumnName, itemKey);
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
        public string GenerateQuery(IDatabaseTableData? tableData, string tableName, uint entry,
            Dictionary<string, DatabaseSolutionItemModifiedField> modifiedFields);
    }
}