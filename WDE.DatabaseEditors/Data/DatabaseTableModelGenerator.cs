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
            Dictionary<ColumnFullName, IDatabaseField> columns = new(ColumnFullNameIgnoreCaseComparer.Instance);

            if (!phantomEntity && key.Count != definition.GroupByKeys.Count)
                throw new Exception($"Trying to create entity with a partial key! (expected: {definition.GroupByKeys.Count} elements in key, got {key.Count})");
            
            foreach (var column in definition.Groups.SelectMany(t => t.Fields)
                .Where(x => x.IsActualDatabaseColumn) // todo: not sure about IsCondition? this method used to create fields with IsCondition even though it makes no sense
                .Distinct(
                    EqualityComparerFactory.Create<DatabaseColumnJson>(
                        f => f.DbColumnName.GetHashCode(),
                        (a, b) => a!.DbColumnName.Equals(b!.DbColumnName))))
            {
                columns[column.DbColumnFullName] = databaseFieldFactory.CreateField(column, column.Default);
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
            IDatabaseSelectResult fieldsFromDb,
            IReadOnlyList<ColumnFullName> selectedColumns,
            IList<IConditionLine>[]? conditionsPerRow)
        {
            HashSet<DatabaseKey> providedKeys = new();

            List<DatabaseEntity> rows = new();
            foreach (var rowIndex in fieldsFromDb)
            {
                DatabaseKey? key = null;
                Dictionary<ColumnFullName, IDatabaseField> columns = new(ColumnFullNameIgnoreCaseComparer.Instance);
                for (int columnIndex = 0; columnIndex < fieldsFromDb.Columns; ++columnIndex)
                {
                    var columnType = fieldsFromDb.ColumnType(columnIndex);

                    var isNull = fieldsFromDb.IsNull(rowIndex, columnIndex);
                    var value = fieldsFromDb.Value(rowIndex, columnIndex);
                    IValueHolder valueHolder = null!;
                    if (columnType == typeof(string))
                    {
                        string? val = fieldsFromDb.Value<string>(rowIndex, columnIndex);
                        valueHolder = new ValueHolder<string>(val, isNull);
                    }
                    else if (columnType == typeof(float))
                    {
                        valueHolder = new ValueHolder<float>(value as float? ?? 0, isNull);
                    }
                    else if (columnType == typeof(decimal))
                    {
                        valueHolder = new ValueHolder<float>(value as float? ?? 0, isNull);
                    }
                    else if (columnType == typeof(uint))
                    {
                        valueHolder = new ValueHolder<long>(value as uint? ?? 0, isNull);
                    }
                    else if (columnType == typeof(int))
                    {
                        valueHolder = new ValueHolder<long>(value as int? ?? 0, isNull);
                    }
                    else if (columnType == typeof(long))
                    {
                        valueHolder = new ValueHolder<long>(value as long? ?? 0, isNull);
                    }
                    else if (columnType == typeof(ulong))
                    {
                        valueHolder = new ValueHolder<long>((long)(value as ulong? ?? 0), isNull);
                    }
                    else if (columnType == typeof(byte))
                    {
                        valueHolder = new ValueHolder<long>(value as byte? ?? 0, isNull);
                    }
                    else if (columnType == typeof(sbyte))
                    {
                        valueHolder = new ValueHolder<long>(value as sbyte? ?? 0, isNull);
                    }
                    else if (columnType == typeof(ushort))
                    {
                        valueHolder = new ValueHolder<long>(value as ushort? ?? 0, isNull);
                    }
                    else if (columnType == typeof(short))
                    {
                        valueHolder = new ValueHolder<long>(value as short? ?? 0, isNull);
                    }
                    else if (columnType == typeof(bool))
                    {
                        valueHolder = new ValueHolder<long>(isNull ? 0 : ((bool)value! ? 1 : 0), isNull);
                    }
                    else if (columnType == typeof(DateTime))
                    {
                        valueHolder = new ValueHolder<long>(isNull ? default : ((DateTimeOffset)(DateTime)value!).ToUnixTimeSeconds(), isNull);
                    }
                    else
                    {
                        throw new NotImplementedException("Unknown column type " + columnType);
                    }

                    columns[selectedColumns[columnIndex]] = databaseFieldFactory.CreateField(selectedColumns[columnIndex], valueHolder);
                }

                key = new DatabaseKey(tableDefinition.GroupByKeys.Select(key =>
                {
                    if (columns[key] is DatabaseField<long> field)
                        return field.Current.Value;
                    throw new Exception("");
                }));
                if (key.HasValue)
                {
                    rows.Add(new DatabaseEntity(true, key.Value, columns, conditionsPerRow?[rowIndex]?.ToList<ICondition>()));
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