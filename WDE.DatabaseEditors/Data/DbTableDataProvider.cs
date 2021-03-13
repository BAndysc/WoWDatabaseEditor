using System;
using System.Collections.Generic;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class DbTableDataProvider : IDbTableDataProvider
    {
        private readonly Lazy<IDbFieldNameSwapDataManager> nameSwapDataManager;

        public DbTableDataProvider(Lazy<IDbFieldNameSwapDataManager> nameSwapDataManager)
        {
            this.nameSwapDataManager = nameSwapDataManager;
        }
        
        public IDbTableData GetDatabaseTable(in DatabaseEditorTableDefinitionJson tableDefinition, IDbTableFieldFactory fieldFactory, Dictionary<string, object> fieldsFromDb)
        {
            if (!string.IsNullOrWhiteSpace(tableDefinition.NameSwapFilePath))
                nameSwapDataManager.Value.RegisterSwapDefinition(tableDefinition.Name, tableDefinition.NameSwapFilePath);
            
            var tableCategories = new List<IDbTableColumnCategory>(tableDefinition.Groups.Count);
            var tableIndex = fieldsFromDb[tableDefinition.TablePrimaryKeyColumnName].ToString();
            var descNameField = fieldsFromDb.ContainsKey(tableDefinition.TableNameSource)
                ? fieldsFromDb[tableDefinition.TableNameSource].ToString()
                : "";
            var tableDesc = $"Template of {descNameField ?? "Unk"}";

            foreach (var category in tableDefinition.Groups)
                tableCategories.Add(CreateCategory(in category, fieldFactory, fieldsFromDb));

            return new DbTableData(tableDefinition.Name, tableDefinition.TableName, tableDefinition.TablePrimaryKeyColumnName, 
                tableIndex ?? "Unk", tableDesc, tableCategories);
        }

        private DbTableColumnCategory CreateCategory(in DbEditorTableGroupJson groupDefinition, IDbTableFieldFactory fieldFactory, Dictionary<string, object> fieldsFromDb)
        {
            var fields = new List<IDbTableField>(groupDefinition.Fields.Count);
            foreach (var field in groupDefinition.Fields)
                fields.Add(fieldFactory.CreateField(in field, fieldsFromDb[field.DbColumnName]));
            
            return new DbTableColumnCategory(groupDefinition.Name, fields);
        }
    }
}