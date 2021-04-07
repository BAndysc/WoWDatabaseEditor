using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DbEditorsSolutionItemSqlProvider : ISolutionItemSqlProvider<DbEditorsSolutionItem>
    {
        private readonly Lazy<IDbTableDefinitionProvider> tableDefinitionProvider;
        private readonly Lazy<IDbEditorTableDataProvider> tableDataProvider;

        public DbEditorsSolutionItemSqlProvider(Lazy<IDbTableDefinitionProvider> tableDefinitionProvider,
            Lazy<IDbEditorTableDataProvider> tableDataProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.tableDataProvider = tableDataProvider;
        }

        public async Task<string> GenerateSql(DbEditorsSolutionItem item)
        {
            // missing table data we have to load it
            if (item.IsMultiRecord)
            {
                if (item.TableData == null)
                    item.CacheTableData(await LoadTable(item.TableContentType, item.Entry));

                if (item.TableData is DbMultiRecordTableData multiRecordTableData)
                    return MultiRecordTableSqlGenerator.GenerateSql(multiRecordTableData);
            }
            
            var tableDefinition = GetTableDefinition(item.TableContentType);
            return GenerateUpdateQuery(item.ModifiedFields, tableDefinition.TableName,
                tableDefinition.TablePrimaryKeyColumnName, item.Entry, item.IsMultiRecord);
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

        private DatabaseEditorTableDefinitionJson GetTableDefinition(DbTableContentType tableContentType)
        {
            switch (tableContentType)
            {
                case DbTableContentType.CreatureTemplate:
                    return tableDefinitionProvider.Value.GetCreatureTemplateDefinition();
                case DbTableContentType.CreatureLootTemplate:
                    return tableDefinitionProvider.Value.GetCreatureLootTemplateDefinition();
                case DbTableContentType.GameObjectTemplate:
                    return tableDefinitionProvider.Value.GetGameobjectTemplateDefinition();
                default:
                    throw new Exception("[DbEditorsSolutionItemSqlProvider] not defined table content type!");
            }
        }

        private Task<IDbTableData?> LoadTable(DbTableContentType tableContentType, uint key)
        {
            switch (tableContentType)
            {
                case DbTableContentType.CreatureLootTemplate:
                    return tableDataProvider.Value.LoadCreatureLootTemplateData(key);
                default:
                    throw new Exception("[DbEditorsSolutionItemSqlProvider] not defined table content type!");
            }
        }
    }
}