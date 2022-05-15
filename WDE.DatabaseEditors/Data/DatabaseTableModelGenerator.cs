using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
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
        private readonly IParameterFactory parameterFactory;
        private readonly IDatabaseFieldFactory databaseFieldFactory;

        public DatabaseTableModelGenerator(IMessageBoxService messageBoxService,
            IParameterFactory parameterFactory,
            IDatabaseFieldFactory databaseFieldFactory)
        {
            this.messageBoxService = messageBoxService;
            this.parameterFactory = parameterFactory;
            this.databaseFieldFactory = databaseFieldFactory;
        }
        
        public DatabaseEntity CreateEmptyEntity(DatabaseTableDefinitionJson definition, DatabaseKey key, bool phantomEntity)
        {
            Dictionary<string, IDatabaseField> columns = new(StringComparer.InvariantCultureIgnoreCase);

            if (!phantomEntity && key.Count != definition.GroupByKeys.Count)
                throw new Exception($"Trying to create entity with a partial key! (expected: {definition.GroupByKeys.Count} elements in key, got {key.Count})");
            
            foreach (var column in definition.Groups.SelectMany(t => t.Fields)
                .Distinct(
                    EqualityComparerFactory.Create<DatabaseColumnJson>(
                        f => f.DbColumnName.GetHashCode(),
                        (a, b) => a!.DbColumnName.Equals(b!.DbColumnName))))
            {
                IValueHolder valueHolder;
                var type = column.ValueType;

                if (parameterFactory.IsRegisteredLong(type))
                    type = "uint";
                else if (parameterFactory.IsRegisteredString(type))
                    type = "string";
                else if (type.EndsWith("Parameter"))
                    type = "uint";
                
                if (type == "float")
                {
                    valueHolder = new ValueHolder<float>(column.Default is float f ? f : 0.0f, column.CanBeNull && column.Default == null);
                }
                else if (type is "int" or "uint" or "long")
                {
                    valueHolder = new ValueHolder<long>(column.Default is long f ? f : 0, column.CanBeNull && column.Default == null);
                }
                else
                    valueHolder = new ValueHolder<string>(column.Default is string f ? f : "", column.CanBeNull && column.Default == null);
                
                columns[column.DbColumnName] = databaseFieldFactory.CreateField(column.DbColumnName, valueHolder);
            }

            Debug.Assert(phantomEntity == key.IsPhantomKey);
            if (!phantomEntity)
            {
                int keyIndex = 0;
                foreach (var name in definition.GroupByKeys)
                {
                    if (columns[name] is not DatabaseField<long> field)
                        throw new Exception("Only long keys are supported now");
                    field.Current.Value = key[keyIndex++];
                }   
            }

            return new DatabaseEntity(false, key, columns, null);
        }
        
        public IDatabaseTableData? CreateDatabaseTable(DatabaseTableDefinitionJson tableDefinition,
            DatabaseKey[]? keys,
            IList<Dictionary<string, (System.Type type, object value)>> fieldsFromDb)
        {
            HashSet<DatabaseKey> providedKeys = new();

            IList<IConditionLine>? conditions = null;
            List<DatabaseEntity> rows = new();
            foreach (var entity in fieldsFromDb)
            {
                DatabaseKey? key = null;
                Dictionary<string, IDatabaseField> columns = new(StringComparer.InvariantCultureIgnoreCase);
                foreach (var column in entity)
                {
                    IValueHolder valueHolder = null!;
                    if (column.Value.type == typeof(string))
                    {
                        string? val = column.Value.value as string ?? null;
                        valueHolder = new ValueHolder<string>(val, column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(float))
                    {
                        valueHolder = new ValueHolder<float>(column.Value.value as float? ?? 0, column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(uint))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as uint? ?? 0, column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(int))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as int? ?? 0, column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(long))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as long? ?? 0, column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(ulong))
                    {
                        valueHolder = new ValueHolder<long>((long)(column.Value.value as ulong? ?? 0), column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(byte))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as byte? ?? 0, column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(sbyte))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as sbyte? ?? 0, column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(ushort))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as ushort? ?? 0, column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(short))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value as short? ?? 0, column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(bool))
                    {
                        valueHolder = new ValueHolder<long>(column.Value.value is DBNull ? 0 : ((bool)column.Value.value ? 1 : 0), column.Value.value is DBNull);
                    }
                    else if (column.Value.type == typeof(IList<IConditionLine>))
                    {
                        conditions = ((IList<IConditionLine>)column.Value.value);
                        continue;
                    }
                    else
                    {
                        throw new NotImplementedException("Unknown column type " + column.Value.type);
                    }

                    columns[column.Key] = databaseFieldFactory.CreateField(column.Key, valueHolder);
                }

                key = new DatabaseKey(tableDefinition.GroupByKeys.Select(key =>
                {
                    if (columns[key] is DatabaseField<long> field)
                        return field.Current.Value;
                    throw new Exception("");
                }));
                if (key.HasValue)
                {
                    rows.Add(new DatabaseEntity(true, key.Value, columns, conditions?.ToList<ICondition>()));
                    providedKeys.Add(key.Value);
                }
            }

            if (tableDefinition.RecordMode == RecordMode.Template)
            {
                Debug.Assert(keys != null);
                foreach (var key in keys)
                {
                    if (!providedKeys.Contains(key))
                        rows.Add(CreateEmptyEntity(tableDefinition, key, false));
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