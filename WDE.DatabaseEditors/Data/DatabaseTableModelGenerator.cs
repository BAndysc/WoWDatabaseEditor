using System;
using System.Collections.Generic;
using WDE.Common.Services.MessageBox;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class DatabaseTableModelGenerator : IDatabaseTableModelGenerator
    {
        private readonly IDatabaseFieldFactory tableFieldFactory;
        private readonly IDatabaseColumnFactory tableColumnFactory;
        private readonly IMessageBoxService messageBoxService;

        public DatabaseTableModelGenerator(IDatabaseFieldFactory tableFieldFactory, IDatabaseColumnFactory tableColumnFactory,
            IMessageBoxService messageBoxService)
        {
            this.tableFieldFactory = tableFieldFactory;
            this.tableColumnFactory = tableColumnFactory;
            this.messageBoxService = messageBoxService;
        }
        
        public IDatabaseTableData? GetDatabaseTable(in DatabaseTableDefinitionJson tableDefinition,
            Dictionary<string, object> fieldsFromDb)
        {
            var tableCategories = new List<IDatabaseFieldsGroup>(tableDefinition.Groups.Count);
            var tableIndex = fieldsFromDb[tableDefinition.TablePrimaryKeyColumnName].ToString();

            try
            {
                foreach (var category in tableDefinition.Groups)
                    tableCategories.Add(CreateCategory(in category, fieldsFromDb));

                return new DatabaseTableData(tableDefinition.Name, tableDefinition.TableName, tableDefinition.TablePrimaryKeyColumnName, 
                    tableIndex ?? "Unk", tableCategories);
            }
            catch (Exception e)
            {
                // in case of throw from DbTableFieldFactory
                ShowLoadingError(e.Message);
            }
            
            return null;
        }

        public IDatabaseTableData? GetDatabaseMultiRecordTable(uint key, in DatabaseTableDefinitionJson tableDefinition,
            IList<Dictionary<string, object>> records)
        {
            // no support for swap data (at least for now) cuz simply no need for that
            // if (!string.IsNullOrWhiteSpace(tableDefinition.NameSwapFilePath))
            //     nameSwapDataManager.Value.RegisterSwapDefinition(tableDefinition.Name, tableDefinition.NameSwapFilePath);

            try
            {
                var columns = new List<IDatabaseColumn>(tableDefinition.Groups[0].Fields.Count);
                var group = tableDefinition.Groups[0];
                // prepare columns
                for (int i = 0; i < group.Fields.Count; ++i)
                {
                    // ensure that each added field of table index column has value of key
                    object? defaultValue = group.Fields[i].DbColumnName == tableDefinition.TablePrimaryKeyColumnName
                        ? key
                        : null;
                    columns.Insert(i, tableColumnFactory.CreateColumn(group.Fields[i], defaultValue));
                }

                foreach (var record in records)
                {
                    for (int i = 0; i < group.Fields.Count; ++i)
                    {
                        var field = group.Fields[i];
                        columns[i].Fields.Add(tableFieldFactory.CreateField(in field, record[field.DbColumnName], columns[i]));
                    }
                }
                
                // make sure all columns have same amount of records
                var firstColumnRecordsAmount = columns[0].Fields.Count;
                foreach (var column in columns)
                {
                    if (column.Fields.Count != firstColumnRecordsAmount)
                        throw new Exception("Detected row amount mismatch between table's columns!");
                }
                
                return new DatabaseMultiRecordTableData(tableDefinition.Name, tableDefinition.TableName,
                    tableDefinition.TablePrimaryKeyColumnName,
                    key.ToString(), columns);
            }
            catch (Exception e)
            {
                // in case of throw from DbTableFieldFactory
                ShowLoadingError(e.Message);
            }

            return null;
        }

        private IDatabaseFieldsGroup CreateCategory(in DatabaseColumnsGroupJson groupDefinition, 
            Dictionary<string, object> fieldsFromDb)
        {
            var fields = new List<IDatabaseField>(groupDefinition.Fields.Count);
            foreach (var field in groupDefinition.Fields)
                fields.Add(tableFieldFactory.CreateField(in field, fieldsFromDb[field.DbColumnName]));
            
            return new DatabaseFieldsGroup(groupDefinition.Name, fields);
        }
        
        private void ShowLoadingError(string msg)
        {
            messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error!")
                .SetMainInstruction(msg)
                .SetIcon(MessageBoxIcon.Error)
                .WithOkButton(true)
                .Build());
        }
    }
}