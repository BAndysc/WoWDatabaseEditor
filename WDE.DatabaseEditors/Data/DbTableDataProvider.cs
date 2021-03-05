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
        public DbTableDataProvider()
        {
            
        }
        
        public IDbTableData GetDatabaseTable(in DatabaseEditorTableDefinitionJson tableDefinition, IDbTableFieldFactory fieldFactory, Dictionary<string, object> fieldsFromDb)
        {
            var tableCategories = new List<IDbTableColumnCategory>(tableDefinition.Groups.Count);
            foreach (var category in tableDefinition.Groups)
                tableCategories.Add(CreateCategory(in category, fieldFactory, fieldsFromDb));
            
            return new DbTableData(tableDefinition.Name, tableCategories);
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