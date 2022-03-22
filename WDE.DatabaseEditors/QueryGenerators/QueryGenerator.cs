using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Expressions;
using WDE.DatabaseEditors.Extensions;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.QueryGenerators
{
    [SingleInstance]
    [AutoRegister]
    public class QueryGenerator : IQueryGenerator
    {
        private readonly ICreatureStatCalculatorService calculatorService;
        private readonly IParameterFactory parameterFactory;
        private readonly IConditionQueryGenerator conditionQueryGenerator;

        public QueryGenerator(ICreatureStatCalculatorService calculatorService,
            IParameterFactory parameterFactory,
            IConditionQueryGenerator conditionQueryGenerator)
        {
            this.calculatorService = calculatorService;
            this.parameterFactory = parameterFactory;
            this.conditionQueryGenerator = conditionQueryGenerator;
        }
        
        public IQuery GenerateQuery(IReadOnlyList<DatabaseKey> keys, IReadOnlyList<DatabaseKey>? deletedKeys, IDatabaseTableData tableData)
        {
            if (tableData.TableDefinition.IsOnlyConditionsTable)
                return BuildConditions(keys, tableData);
            if (tableData.TableDefinition.RecordMode == RecordMode.MultiRecord)
                return GenerateInsertQuery(keys, tableData);
            if (tableData.TableDefinition.RecordMode == RecordMode.SingleRow)
                return GenerateSingleRecordQuery(keys, deletedKeys, tableData);
            return GenerateUpdateQuery(tableData);
        }
        
        public IQuery GenerateSingleRecordQuery(IReadOnlyList<DatabaseKey> keys, IReadOnlyList<DatabaseKey>? deletedKeys, IDatabaseTableData tableData)
        {
            var query = Queries.BeginTransaction();

            // delete
            GeneratePrimaryKeyDeletion(tableData.TableDefinition, deletedKeys, query);
            
            //insert
            query.Add(GenerateInsertQuery(tableData.Entities.Where(e => !e.ExistInDatabase).Select(e => e.GenerateKey(tableData.TableDefinition)).ToList(), tableData));
            
            //update
            foreach (var entity in tableData.Entities)
            {
                if (entity.ExistInDatabase)
                    query.Add(GenerateUpdateQuery(tableData.TableDefinition, entity));
            }

            return query.Close();
        }

        private static IWhere GenerateWherePrimaryKey(IList<string> keys, ITable table, DatabaseKey key)
        {
            Debug.Assert(keys.Count == key.Count);

            if (key.Count == 1)
                return table.Where(r => r.Column<long>(keys[0]) == key[0]);
            else
            {
                var where = table.Where(r => r.Column<long>(keys[0]) == key[0]);
                for (int i = 1; i < keys.Count; i++)
                {
                    int index = i;
                    where = where.Where(r => r.Column<long>(keys[index]) == key[index]);
                }

                return where;
            }
        }
        
        private static IWhere GenerateWherePrimaryKey(DatabaseTableDefinitionJson definition, ITable table, DatabaseKey key)
        {
            return GenerateWherePrimaryKey(definition.GroupByKeys, table, key);
        }

        private static void GeneratePrimaryKeyDeletion(DatabaseTableDefinitionJson definition, IReadOnlyList<DatabaseKey>? keys,
            IMultiQuery query)
        {
            if (keys == null || keys.Count == 0)
                return;

            ITable table;

            if (definition.ForeignTable != null)
            {
                foreach (var foreign in definition.ForeignTable)
                {
                    table = query.Table(foreign.TableName);
                    if (keys.Count > 1 && keys[0].Count == 1)
                        table.WhereIn(foreign.ForeignKeys[0], keys.Select(k => k[0])).Delete();
                    else
                    {
                        foreach (var key in keys)
                            GenerateWherePrimaryKey(foreign.ForeignKeys, table, key).Delete();
                    }
                }
            }
            
            table = query.Table(definition.TableName);
            if (keys.Count > 1 && keys[0].Count == 1)
                table.WhereIn(definition.TablePrimaryKeyColumnName, keys.Select(k => k[0])).Delete();
            else
            {
                foreach (var key in keys)
                    GenerateWherePrimaryKey(definition, table, key).Delete();
            }
        }

        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, IReadOnlyList<DatabaseKey> keys)
        {
            var query = Queries.BeginTransaction();
            GeneratePrimaryKeyDeletion(table, keys.Distinct().ToList(), query);
            return query.Close();
        }

        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity) =>
            GenerateDeleteQuery(table, entity.GenerateKey(table));
        
        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseKey key)
        {
            var query = Queries.BeginTransaction();
            if (table.ForeignTable != null)
            {
                foreach (var foreign in table.ForeignTable)
                {
                    var tableKey = foreign.ForeignKeys[0];
                    GenerateWherePrimaryKey(foreign.ForeignKeys.Take(table.GroupByKeys.Count).ToList(), query
                            .Table(foreign.TableName), key).Delete();
                }
            }

            GenerateWherePrimaryKey(table, query.Table(table.TableName), key).Delete();
            return query.Close();
        }

        private class EntityComparer : IComparer<DatabaseEntity>
        {
            private readonly DatabaseTableDefinitionJson definition;

            public EntityComparer(DatabaseTableDefinitionJson definition)
            {
                this.definition = definition;
            }

            public int Compare(DatabaseEntity? x, DatabaseEntity? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;

                var sortByList = definition.SortBy ?? definition.PrimaryKey;
                
                if (sortByList == null || sortByList.Count == 0)
                    return 0;

                foreach (var sortBy in sortByList)
                {
                    var comparisonResult = x.GetCell(sortBy)?.CompareTo(y.GetCell(sortBy)) ?? 0;
                    if (comparisonResult != 0) 
                        return comparisonResult;
                }
                
                var existInDatabaseComparison = x.ExistInDatabase.CompareTo(y.ExistInDatabase);
                if (existInDatabaseComparison != 0) return existInDatabaseComparison;
                return x.Phantom || y.Phantom ? 0 : x.Key.CompareTo(y.Key);
            }
        }

        private IQuery GenerateInsertQuery(IReadOnlyList<DatabaseKey> keys, IDatabaseTableData tableData)
        {
            if (keys.Count == 0)
                return Queries.Empty();

            IMultiQuery query = Queries.BeginTransaction();
            
            query.Add(GenerateDeleteQuery(tableData.TableDefinition, keys));
            
            if (tableData.Entities.Count == 0)
                return query.Close();

            var columns = tableData.TableDefinition.TableColumns
                .Select(c => c.Value)
                .Where(col => !col.IsMetaColumn && !col.IsConditionColumn)
                .GroupBy(columns => columns.ForeignTable ?? tableData.TableDefinition.TableName)
                .ToDictionary(g => g.Key, g => g.ToList());

            HashSet<EntityKey> entityKeys = new();
            Dictionary<string, List<Dictionary<string, object?>>> inserts = new(tableData.Entities.Count);
            
            List<string> duplicates = new List<string>();
            var comparer = new EntityComparer(tableData.TableDefinition);
            foreach (var entity in tableData.Entities.OrderBy(t => t, comparer))
            {
                if (!entity.Phantom && !keys.Contains(entity.Key))
                    continue;
                
                bool duplicate = tableData.TableDefinition.PrimaryKey != null && !entityKeys.Add(new EntityKey(entity, tableData.TableDefinition));
                foreach (var table in columns)
                {
                    bool isDefault = true;
                    bool isMainTable = table.Key == tableData.TableDefinition.TableName;
                    var cells = table.Value.ToDictionary(c => c.DbColumnName, c =>
                    {
                        var cell = entity.GetCell(c.DbColumnName)!;
                        if (c.AutogenerateComment != null && cell is DatabaseField<string> sField)
                        {
                            var evaluator = new DatabaseExpressionEvaluator(calculatorService, parameterFactory, tableData.TableDefinition, c.AutogenerateComment!);
                            var comment = evaluator.Evaluate(entity);
                            if (comment is string s)
                                return s.AddComment(sField.Current.Value);
                        }

                        var columnDefinition = tableData.TableDefinition.TableColumns[cell.FieldName];
                        if (!columnDefinition.CanBeNull && cell.Object is null)
                            return columnDefinition.Default ?? 0L;
                        if (columnDefinition.CanBeNull && cell.Object is null)
                            return null;
                        if (!isMainTable && columnDefinition.IsTypeFloat && ((cell.Object == null && columnDefinition.Default != null) || cell.Object != null && !cell.Object.Equals(columnDefinition.Default ?? 0f)))
                            isDefault = false;
                        if (!isMainTable && columnDefinition.IsTypeLong && ((cell.Object == null && columnDefinition.Default != null) || cell.Object != null && !cell.Object.Equals(columnDefinition.Default ?? 0L)))
                            isDefault = false;
                        return FixUnixTimestampAndNullability(columnDefinition, cell.Object);
                    });
                    if (!isMainTable)
                    {
                        if (isDefault)
                            continue;

                        var newCells = new Dictionary<string, object?>();
                        var foreignKeys = tableData.TableDefinition.ForeignTableByName![table.Key].ForeignKeys;
                        for (int i = 0; i < foreignKeys.Length; ++i)
                            newCells[foreignKeys[i]] = entity.GetTypedValueOrThrow<long>(tableData.TableDefinition.PrimaryKey![i]);
                        foreach (var old in cells)
                            newCells[old.Key] = old.Value;
                        cells = newCells;
                    }

                    if (duplicate)
                    {
                        if (table.Key == tableData.TableDefinition.TableName)
                            duplicates.Add("(" + string.Join(", ", cells.Values) + ")");
                    }
                    else
                    {
                        if (!inserts.TryGetValue(table.Key, out var insertList))
                            insertList = inserts[table.Key] = new();
                        insertList.Add(cells);
                    }
                }
            }

            foreach (var i in inserts)
            {
                query.Table(i.Key)
                    .BulkInsert(i.Value);   
            }

            if (duplicates.Count > 0)
            {
                query.Comment("duplicates, cannot insert:");
                foreach (var line in duplicates)
                    query.Comment(line);
            }

            query.Add(BuildConditions(keys, tableData));

            return query.Close();
        }

        private IQuery BuildConditions(IReadOnlyList<DatabaseKey> keys, IDatabaseTableData tableData)
        {
            if (tableData.TableDefinition.Condition == null)
                return Queries.Empty();
            
            IMultiQuery query = Queries.BeginTransaction();
            query.Add(BuildConditionsDeleteQuery(keys, tableData));
            List<IConditionLine> conditions = new();
            int sourceType = tableData.TableDefinition.Condition.SourceType;

            foreach (var entity in tableData.Entities)
            {
                if (entity.Conditions == null)
                    continue;

                int sourceGroup = 0;
                int sourceEntry = 0;
                int sourceId = 0;

                if (tableData.TableDefinition.Condition.SourceEntryColumn != null &&
                    entity.GetCell(tableData.TableDefinition.Condition.SourceEntryColumn) is DatabaseField<long>
                        entryCell)
                    sourceEntry = (int) entryCell.Current.Value;

                if (tableData.TableDefinition.Condition.SourceGroupColumn != null &&
                    entity.GetCell(tableData.TableDefinition.Condition.SourceGroupColumn) is DatabaseField<long>
                        groupCell)
                    sourceGroup = (int) groupCell.Current.Value;

                if (tableData.TableDefinition.Condition.SourceIdColumn != null &&
                    entity.GetCell(tableData.TableDefinition.Condition.SourceIdColumn) is DatabaseField<long>
                        idCell)
                    sourceId = (int) idCell.Current.Value;

                foreach (var condition in entity.Conditions)
                    conditions.Add(new AbstractConditionLine(sourceType, sourceGroup, sourceEntry, sourceId, condition));
            }
            query.Add(conditionQueryGenerator.BuildInsertQuery(conditions));

            return query.Close();
        }

        private IQuery BuildConditionsDeleteQuery(IReadOnlyList<DatabaseKey> keys, IDatabaseTableData tableData)
        {
            if (tableData.TableDefinition.Condition == null)
                return Queries.Empty();

            string? columnKey = null;

            if (tableData.TableDefinition.Condition.SourceEntryColumn ==
                tableData.TableDefinition.TablePrimaryKeyColumnName)
                columnKey = "SourceEntry";
            else if (tableData.TableDefinition.Condition.SourceGroupColumn ==
                     tableData.TableDefinition.TablePrimaryKeyColumnName)
                columnKey = "SourceGroup";
            else if (tableData.TableDefinition.Condition.SourceIdColumn ==
                     tableData.TableDefinition.TablePrimaryKeyColumnName)
                columnKey = "SourceId";

            if (columnKey == null)
                throw new Exception("No condition source group/entry/id is table primary key. Unable to generate SQL.");

            return Queries.Table("conditions")
                .Where(r => r.Column<int>("SourceTypeOrReferenceId") == tableData.TableDefinition.Condition.SourceType)
                .WhereIn(columnKey, keys.Distinct())
                .Delete();
        }

        public IQuery GenerateUpdateFieldQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity,
            IDatabaseField field)
        {
            var column = table.TableColumns[field.FieldName];

            var q = Queries.Table(column.ForeignTable ?? table.TableName);
            var where = GenerateConditionsForSingleRow(q, table, column.ForeignTable ?? table.TableName, entity);
            return where
                .Set(field.FieldName, FixUnixTimestampAndNullability(column, field.Object))
                .Update();
        }

        private object? FixUnixTimestampAndNullability(DatabaseColumnJson column, object? value)
        {
            if (!column.CanBeNull && value == null)
            {
                if (column.Default != null)
                    return column.Default;
                if (column.IsTypeLong)
                    return 0L;
                if (column.IsTypeFloat)
                    return 0f;
                if (column.IsTypeString)
                    return "";
            }
            if (!column.IsUnixTimestamp)
                return value;
            if (value is long l)
                return new SqlTimestamp(l);
            return value;
        }

        private IQuery GenerateUpdateQuery(DatabaseTableDefinitionJson definition, DatabaseEntity entity)
        {
            IMultiQuery query = Queries.BeginTransaction();
            Dictionary<string, List<IDatabaseField>> fieldsByTable = entity.Fields
                .Select(ef => (ef, definition.TableColumns[ef.FieldName]))
                .Where(pair => !pair.Item2.IsMetaColumn && !pair.Item2.IsConditionColumn)
                .GroupBy(pair => pair.Item2.ForeignTable ?? definition.TableName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ef).ToList());

            if (definition.ForeignTable != null)
            {
                foreach (var foreign in definition.ForeignTable)
                {
                    for (var index = foreign.ForeignKeys.Length - 1; index >= 0; index--)
                    {
                        var foreignKey = foreign.ForeignKeys[index];
                        var thisKey = definition.PrimaryKey![index];
                        
                        fieldsByTable[foreign.TableName].Insert(0,
                            new DatabaseField<long>(foreignKey, new ValueHolder<long>(entity.GetTypedValueOrThrow<long>(thisKey), false)));
                    }
                }
            }
            
            if (entity.ExistInDatabase)
            {
                foreach (var table in fieldsByTable)
                {
                    if (!table.Value.Any(f => f.IsModified))
                        continue;

                    var updates = table.Value
                        .Where(f => f.IsModified)
                        .ToList();
                    
                    IList<string> groupByKeys = definition.GroupByKeys;
                    if (table.Key != definition.TableName)
                    {
                        groupByKeys = definition.ForeignTableByName![table.Key].ForeignKeys.Take(groupByKeys.Count).ToList();
                        query.Table(table.Key)
                            .InsertIgnore(
                                definition.ForeignTableByName[table.Key].ForeignKeys
                                    .Zip(definition.PrimaryKey!)
                                    .ToDictionary<(string, string), string, object?>(
                                    key => key.Item1,
                                    key => entity.GetTypedValueOrThrow<long>(key.Item2))
                                );
                    }

                    var where = GenerateWherePrimaryKey(groupByKeys, query.Table(table.Key), entity.Key);
                    IUpdateQuery update = where
                        .Set(updates[0].FieldName, FixUnixTimestampAndNullability(definition.TableColumns[updates[0].FieldName], updates[0].Object));
                    for (int i = 1; i < updates.Count; ++i)
                        update = update.Set(updates[i].FieldName, FixUnixTimestampAndNullability(definition.TableColumns[updates[i].FieldName], updates[i].Object));

                    update.Update();
                }
            }
            else
            {
                foreach (var table in fieldsByTable.Reverse())
                {
                    var where = GenerateConditionsForSingleRow(query.Table(table.Key), definition, table.Key, entity);
                    where.Delete();
                }
                foreach (var table in fieldsByTable)
                {
                    if (table.Key == definition.TableName)
                    {
                        query.Table(table.Key)
                            .Insert(table.Value.ToDictionary(t => t.FieldName, t => t.Object));
                    }
                    else
                    {
                        var isModified = table.Value.Any(f => f.IsModified);
                        if (isModified)
                        {
                            var updates = table.Value
                                .Where(f => f.IsModified)
                                .ToList();
                            var primaryKeyColumn = definition.ForeignTableByName![table.Key].ForeignKeys.Take(definition.GroupByKeys.Count).ToList();
                            query.Table(table.Key)
                                .InsertIgnore(
                                    definition.ForeignTableByName[table.Key].ForeignKeys
                                        .Zip(definition.PrimaryKey!)
                                        .ToDictionary<(string, string), string, object?>(
                                            key => key.Item1,
                                            key => entity.GetTypedValueOrThrow<long>(key.Item2))
                                );
                            var where = GenerateWherePrimaryKey(primaryKeyColumn, query.Table(table.Key), entity.GenerateKey(definition));
                            IUpdateQuery update = where
                                .Set(updates[0].FieldName, FixUnixTimestampAndNullability(definition.TableColumns[updates[0].FieldName], updates[0].Object));
                            for (int i = 1; i < updates.Count; ++i)
                                update = update.Set(updates[i].FieldName, FixUnixTimestampAndNullability(definition.TableColumns[updates[i].FieldName], updates[i].Object));

                            update.Update();
                        }
                    }
                }
            }

            return query.Close();
        }
        
        private IQuery GenerateUpdateQuery(IDatabaseTableData tableData)
        {
            IMultiQuery query = Queries.BeginTransaction();
            
            foreach (var entity in tableData.Entities)
            {
                query.Add(GenerateUpdateQuery(tableData.TableDefinition, entity));
            }

            return query.Close();
        }

        private IWhere GenerateConditionsForSingleRow(ITable table, DatabaseTableDefinitionJson definition, string tableName, DatabaseEntity entity)
        {
            // todo: this might not be good after change to DatabaseKeys
            if (tableName == definition.TableName)
            {
                return GenerateWherePrimaryKey(definition, table, entity.GenerateKey(definition));
            } 
            else
            {
                var foreignKeys = definition.ForeignTableByName![tableName].ForeignKeys;

                var where = table
                    .Where(row =>
                        row.Column<uint>(foreignKeys[0]) ==
                        (uint)entity.GetTypedValueOrThrow<long>(definition.PrimaryKey![0]));
                            
                for (int i = 1; i < foreignKeys.Length; ++i)
                {
                    var foreignKey = foreignKeys[i];
                    var thisKey = definition.PrimaryKey![i];
                    where = where.Where(row =>
                        row.Column<uint>(foreignKey) == (uint)entity.GetTypedValueOrThrow<long>(thisKey));
                }

                return where;
            }
        }

        private class EntityKey
        {
            private readonly IList<IDatabaseField> fields;
            private readonly int hash;
            
            public EntityKey(DatabaseEntity entity, DatabaseTableDefinitionJson table)
            {
                fields = table.PrimaryKey!.Select(key => entity.GetCell(key)!).ToList();
                hash = 0;
                foreach (var field in fields)
                    hash = HashCode.Combine(hash, field.GetHashCode());
            }

            private bool Equals(EntityKey other)
            {
                if (other.fields.Count != fields.Count)
                    return false;
                for (int i = 0; i < fields.Count; ++i)
                    if (!fields[i].Equals(other.fields[i]))
                        return false;
                return true;
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((EntityKey) obj);
            }

            public override int GetHashCode() => hash;
        }
    }
}