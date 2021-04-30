using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Services.MessageBox;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Utils;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class DatabaseTableModelGenerator : IDatabaseTableModelGenerator
    {
        private readonly IMessageBoxService messageBoxService;
        private readonly IDatabaseFieldFactory databaseFieldFactory;

        public DatabaseTableModelGenerator(IMessageBoxService messageBoxService,
            IDatabaseFieldFactory databaseFieldFactory)
        {
            this.messageBoxService = messageBoxService;
            this.databaseFieldFactory = databaseFieldFactory;
        }
        
        public DatabaseEntity CreateEmptyEntity(DatabaseTableDefinitionJson definition, uint key)
        {
            Dictionary<string, IDatabaseField> columns = new();
            
            foreach (var column in definition.Groups.SelectMany(t => t.Fields)
                .Distinct(
                    EqualityComparerFactory.Create<DatabaseColumnJson>(
                        f => f.DbColumnName.GetHashCode(),
                        (a, b) => a!.DbColumnName.Equals(b!.DbColumnName))))
            {
                IValueHolder valueHolder;
                if (column.ValueType == "float")
                {
                    valueHolder = new ValueHolder<float>(column.Default is float f ? f : 0.0f);
                }
                else if (column.ValueType.EndsWith("Parameter") || column.ValueType == "int" ||
                         column.ValueType == "uint")
                {
                    if (column.DbColumnName == definition.TablePrimaryKeyColumnName)
                        valueHolder = new ValueHolder<long>(key);
                    else
                        valueHolder = new ValueHolder<long>(column.Default is long f ? f : 0);
                }
                else
                    valueHolder = new ValueHolder<string>(column.Default is string f ? f : "");


                columns[column.DbColumnName] = databaseFieldFactory.CreateField(column.DbColumnName, valueHolder);
            }

            return new DatabaseEntity(false, key, columns);
        }
        
        public IDatabaseTableData? CreateDatabaseTable(DatabaseTableDefinitionJson tableDefinition,
            uint[] keys,
            IList<Dictionary<string, (System.Type type, object value)>> fieldsFromDb)
        {
            HashSet<uint> providedKeys = new();
            
            List<DatabaseEntity> rows = new();
            foreach (var entity in fieldsFromDb)
            {
                uint? key = null;
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
                    else if (column.Value.type == typeof(bool))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value is DBNull ? 0 : ((bool)column.Value.value ? 1 : 0));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    if (column.Key == tableDefinition.TablePrimaryKeyColumnName)
                    {
                        if (valueHolder is ValueHolder<long> longKey)
                        {
                            key = (uint)longKey.Value;
                            providedKeys.Add(key.Value);
                        }
                    }
                    
                    columns[column.Key] = databaseFieldFactory.CreateField(column.Key, valueHolder);
                }
                if (key.HasValue)
                    rows.Add(new DatabaseEntity(true, key.Value, columns));
            }

            if (!tableDefinition.IsMultiRecord)
            {
                foreach (var key in keys)
                {
                    if (!providedKeys.Contains(key))
                        rows.Add(CreateEmptyEntity(tableDefinition, key));
                }   
            }

            try
            {
                return new DatabaseTableData(tableDefinition, rows);
            }
            catch (Exception e)
            {
                // in case of throw from DbTableFieldFactory
                ShowLoadingError(e.Message);
            }
            
            return null;
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