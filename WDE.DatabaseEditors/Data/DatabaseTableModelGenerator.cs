using System;
using System.Collections.Generic;
using WDE.Common.Services.MessageBox;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;

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
            IList<Dictionary<string, (System.Type type, object value)>> fieldsFromDb)
        {
            //var tableCategories = new List<IDatabaseFieldsGroup>(tableDefinition.Groups.Count);
            //var tableIndex = fieldsFromDb[tableDefinition.TablePrimaryKeyColumnName].ToString();

            List<DatabaseEntity> rows = new();
            foreach (var entity in fieldsFromDb)
            {
                Dictionary<string, IDatabaseField> columns = new();
                foreach (var column in entity)
                {
                    IValueHolder valueHolder = null!;
                    if (column.Value.type == typeof(string))
                    {
                        valueHolder = new ValueHolder<string>(column.Value.value as string ?? null);
                    }
                    else if (column.Value.type == typeof(float))
                    {
                        valueHolder = new ValueHolder<float>(column.Value.value as float? ?? 0);
                    }
                    else if (column.Value.type == typeof(uint))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as uint? ?? 0);
                    }
                    else if (column.Value.type == typeof(int))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as int? ?? 0);
                    }
                    else if (column.Value.type == typeof(long))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as long? ?? 0);
                    }
                    else if (column.Value.type == typeof(byte))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as byte? ?? 0);
                    }
                    else if (column.Value.type == typeof(sbyte))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as sbyte? ?? 0);
                    }
                    else if (column.Value.type == typeof(ushort))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as ushort? ?? 0);
                    }
                    else if (column.Value.type == typeof(short))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as short? ?? 0);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    columns[column.Key] = tableFieldFactory.CreateField(column.Key, valueHolder);
                }
                rows.Add(new DatabaseEntity(columns));
            }
            
            try
            {
                foreach (var category in tableDefinition.Groups)
                {
                    //tableCategories.Add(CreateCategory(in category, fieldsFromDb));
                }

                return new DatabaseTableData(tableDefinition, rows);
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
                        //columns[i].Fields.Add(tableFieldFactory.CreateField(in field, record[field.DbColumnName], columns[i]));
                    }
                }
                
                // make sure all columns have same amount of records
                var firstColumnRecordsAmount = columns[0].Fields.Count;
                foreach (var column in columns)
                {
                    if (column.Fields.Count != firstColumnRecordsAmount)
                        throw new Exception("Detected row amount mismatch between table's columns!");
                }

                throw new Exception();
                
                //return new DatabaseMultiRecordTableData(tableDefinition.Name, tableDefinition.TableName,
                //    tableDefinition.TablePrimaryKeyColumnName,
                //    key.ToString(), columns);
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
            IObservable<bool>? showGroup = null;
            foreach (var column in groupDefinition.Fields)
            {
                //var field = tableFieldFactory.CreateField(in column, fieldsFromDb[column.DbColumnName]);
                //fields.Add(field);
            }
            
            /*if (groupDefinition.ShowIf.HasValue && 
                groupDefinition.ShowIf.Value.ColumnName == column.DbColumnName &&
                field is DatabaseField<long> longField)
            {
                int showIfValue = groupDefinition.ShowIf.Value.Value;
                showGroup = longField.Parameter.ToObservable(p => p.Value).Select(val => val == showIfValue);
            }*/
            
            return new DatabaseFieldsGroup(groupDefinition.Name, fields, showGroup);
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