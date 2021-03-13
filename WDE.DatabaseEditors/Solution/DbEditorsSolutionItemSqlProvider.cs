using System.Collections.Generic;
using System.Linq;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DbEditorsSolutionItemSqlProvider : ISolutionItemSqlProvider<DbEditorsSolutionItem>
    {
        public string GenerateSql(DbEditorsSolutionItem item) => GenerateQuery(item.ModifiedFields, item.DbTableName, item.KeyColumnName, item.Entry);

        private string GenerateQuery(Dictionary<string, DbTableSolutionItemModifiedField> fields, string tableName, string keyColumnName, uint itemKey)
        {
            if (fields == null || fields.Count == 0)
                return "";

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
    }
}