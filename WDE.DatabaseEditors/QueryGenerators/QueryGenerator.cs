using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Expressions;
using WDE.DatabaseEditors.Extensions;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Services;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.QueryGenerators
{
    [SingleInstance]
    [AutoRegister]
    public class QueryGenerator : IQueryGenerator
    {
        private readonly ICommentGeneratorService commentGeneratorService;
        private readonly IParameterFactory parameterFactory;
        private readonly IQueryGenerator<ConditionDeleteModel> conditionDeleteGenerator;
        private readonly IConditionQueryGenerator conditionQueryGenerator;
        private readonly ICurrentCoreVersion currentCoreVersion;

        private Dictionary<DatabaseTable, ICustomQueryGeneratorAppend> customQueryAppenders = new();
        private Dictionary<DatabaseTable, ICustomFullQueryGenerator> customQueryGenerators = new();

        public QueryGenerator(ICommentGeneratorService commentGeneratorService,
            IParameterFactory parameterFactory,
            IQueryGenerator<ConditionDeleteModel> conditionDeleteGenerator,
            IConditionQueryGenerator conditionQueryGenerator,
            ICurrentCoreVersion currentCoreVersion,
            IEnumerable<ICustomQueryGeneratorAppend> customQueryAppenders,
            IEnumerable<ICustomFullQueryGenerator> customQueryGenerators)
        {
            this.commentGeneratorService = commentGeneratorService;
            this.parameterFactory = parameterFactory;
            this.conditionDeleteGenerator = conditionDeleteGenerator;
            this.conditionQueryGenerator = conditionQueryGenerator;
            this.currentCoreVersion = currentCoreVersion;
            foreach (var gen in customQueryAppenders)
            {
                this.customQueryAppenders[gen.TableName] = gen;
            }
            foreach (var gen in customQueryGenerators)
            {
                this.customQueryGenerators[gen.TableName] = gen;
            }
        }
        
        public IQuery GenerateQuery(IReadOnlyList<DatabaseKey> keys, IReadOnlyList<DatabaseKey>? deletedKeys, IDatabaseTableData tableData)
        {
            if (customQueryGenerators.TryGetValue(tableData.TableDefinition.Id, out var customQueryGenerator))
            {
                return customQueryGenerator.Generate(keys, deletedKeys, tableData);
            }

            if (tableData.TableDefinition.IsOnlyConditionsTable is OnlyConditionMode.IgnoreTableCompletely or OnlyConditionMode.TableReadOnly)
                return BuildConditions(keys, tableData);
            if (tableData.TableDefinition.RecordMode == RecordMode.MultiRecord)
                return GenerateInsertQuery(keys, tableData);
            if (tableData.TableDefinition.RecordMode == RecordMode.SingleRow)
                return GenerateSingleRecordQuery(keys, deletedKeys, tableData);
            return GenerateUpdateQuery(tableData);
        }
        
        public IQuery GenerateSingleRecordQuery(IReadOnlyList<DatabaseKey> keys, IReadOnlyList<DatabaseKey>? deletedKeys, IDatabaseTableData tableData)
        {
            var query = Queries.BeginTransaction(tableData.TableDefinition.DataDatabaseType);

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

            if (customQueryAppenders.TryGetValue(tableData.TableDefinition.Id, out var appender))
            {
                query.Add(appender.Generate(keys, deletedKeys, tableData));
            }
            
            return query.Close();
        }

        private static IWhere GenerateWherePrimaryKey(IList<ColumnFullName> keys, ITable table, DatabaseKey key)
        {
            Debug.Assert(keys.Count == key.Count);

            if (key.Count == 1)
                return table.Where(r => r.Column<long>(keys[0].ColumnName) == key[0]);
            else
            {
                var where = table.Where(r => r.Column<long>(keys[0].ColumnName) == key[0]);
                for (int i = 1; i < keys.Count; i++)
                {
                    int index = i;
                    where = where.Where(r => r.Column<long>(keys[index].ColumnName) == key[index]);
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
                    table = query.Table(new DatabaseTable(definition.DataDatabaseType, foreign.TableName));
                    if (keys.Count > 1 && keys[0].Count == 1)
                    {
                        foreach (var chunk in keys.Select(k => k[0]).Chunk(128))
                            table.WhereIn(foreign.ForeignKeys[0].ColumnName, chunk).Delete();
                    }
                    else
                    {
                        foreach (var key in keys)
                            GenerateWherePrimaryKey(foreign.ForeignKeys, table, key).Delete();
                    }
                }
            }
            
            table = query.Table(definition.Id);
            if (keys.Count > 1 && keys[0].Count == 1)
            {
                foreach (var chunk in keys.Select(k => k[0]).Chunk(128))
                    table.WhereIn(definition.TablePrimaryKeyColumnName!.Value.ColumnName, chunk).Delete();
            }
            else
            {
                foreach (var key in keys)
                    GenerateWherePrimaryKey(definition, table, key).Delete();
            }
        }

        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, IReadOnlyList<DatabaseKey> keys)
        {
            var query = Queries.BeginTransaction(table.DataDatabaseType);
            GeneratePrimaryKeyDeletion(table, keys.Distinct().ToList(), query);
            return query.Close();
        }

        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity) =>
            GenerateDeleteQuery(table, entity.GenerateKey(table));
        
        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseKey key)
        {
            var query = Queries.BeginTransaction(table.DataDatabaseType);
            if (table.ForeignTable != null)
            {
                foreach (var foreign in table.ForeignTable)
                {
                    var tableKey = foreign.ForeignKeys[0];
                    GenerateWherePrimaryKey(foreign.ForeignKeys.Take(table.GroupByKeys.Count).ToList(), query
                            .Table(new DatabaseTable(table.DataDatabaseType, foreign.TableName)), key).Delete();
                }
            }

            GenerateWherePrimaryKey(table, query.Table(table.Id), key).Delete();
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

        public IQuery GenerateInsertQuery(IReadOnlyList<DatabaseKey> keys, IDatabaseTableData tableData)
        {
            if (keys.Count == 0)
                return Queries.Empty(tableData.TableDefinition.DataDatabaseType);

            IMultiQuery query = Queries.BeginTransaction(tableData.TableDefinition.DataDatabaseType);
            
            query.Add(GenerateDeleteQuery(tableData.TableDefinition, keys));
            
            if (tableData.Entities.Count == 0)
                return query.Close();

            var columns = tableData.TableDefinition.TableColumns
                .Select(c => c.Value)
                .Where(col => col.IsActualDatabaseColumn)
                .GroupBy(columns => columns.ForeignTable != null ? new DatabaseTable(tableData.TableDefinition.DataDatabaseType, columns.ForeignTable) : tableData.TableDefinition.Id)
                .ToDictionary(g => g.Key, g => g.ToList());

            HashSet<HashedEntityPrimaryKey> entityKeys = new();
            Dictionary<DatabaseTable, List<Dictionary<string, object?>>> inserts = new(tableData.Entities.Count);
            
            List<string> duplicates = new List<string>();
            var comparer = new EntityComparer(tableData.TableDefinition);
            foreach (var entity in tableData.Entities.OrderBy(t => t, comparer))
            {
                if (!entity.Phantom && !keys.Contains(entity.Key))
                    continue;
                
                bool duplicate = tableData.TableDefinition.PrimaryKey != null && !entityKeys.Add(new HashedEntityPrimaryKey(entity, tableData.TableDefinition));
                foreach (var table in columns)
                {
                    bool isDefault = true;
                    bool isMainTable = table.Key == tableData.TableDefinition.Id;
                    var cells = table.Value.ToDictionary(c => c.DbColumnName, c =>
                    {
                        var cell = entity.GetCell(c.DbColumnFullName)!;
                        if (c.AutogenerateComment != null)
                            return commentGeneratorService.GenerateFinalComment(entity, tableData.TableDefinition, c.DbColumnFullName);

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
                    if (isMainTable)
                    {
                        if (tableData.TableDefinition.AutofillBuildColumn is { } autofillBuildColumn)
                            cells[autofillBuildColumn] = currentCoreVersion.Current.Version.Build;
                    }
                    else
                    {
                        if (isDefault)
                            continue;

                        var newCells = new Dictionary<string, object?>();
                        var foreignTableDefinition = tableData.TableDefinition.ForeignTableByName![table.Key.Table];
                        var foreignKeys = foreignTableDefinition.ForeignKeys;
                        for (int i = 0; i < foreignKeys.Length; ++i)
                            newCells[foreignKeys[i].ColumnName] = entity.GetTypedValueOrThrow<long>(tableData.TableDefinition.PrimaryKey![i]);
                        foreach (var old in cells)
                            newCells[old.Key] = old.Value;
                        if (foreignTableDefinition.AutofillBuildColumn is { } autofillBuildColumn)
                            newCells[autofillBuildColumn] = currentCoreVersion.Current.Version.Build;
                        cells = newCells;
                    }

                    if (duplicate)
                    {
                        if (table.Key == tableData.TableDefinition.Id)
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
                return Queries.Empty(tableData.TableDefinition.DataDatabaseType);
            
            IMultiQuery query = Queries.BeginTransaction(tableData.TableDefinition.DataDatabaseType);

            if (tableData.TableDefinition.RecordMode == RecordMode.SingleRow)
            {
                foreach (var deleteQuery in BuildConditionsDeleteQuerySingleRow(keys, tableData))
                    query.Add(deleteQuery);
            }
            else
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
                    entity.GetCell(tableData.TableDefinition.Condition.SourceEntryColumn.Value) is DatabaseField<long>
                        entryCell)
                    sourceEntry = (int) entryCell.Current.Value;

                if (tableData.TableDefinition.Condition.SourceGroupColumn != null &&
                    entity.GetCell(tableData.TableDefinition.Condition.SourceGroupColumn.Name) is DatabaseField<long>
                        groupCell)
                    sourceGroup = tableData.TableDefinition.Condition.SourceGroupColumn.Calculate((int)groupCell.Current.Value);

                if (tableData.TableDefinition.Condition.SourceIdColumn != null &&
                    entity.GetCell(tableData.TableDefinition.Condition.SourceIdColumn.Value) is DatabaseField<long>
                        idCell)
                    sourceId = (int) idCell.Current.Value;

                foreach (var condition in entity.Conditions)
                    conditions.Add(new AbstractConditionLine(sourceType, sourceGroup, sourceEntry, sourceId, condition));
            }
            query.Add(conditionQueryGenerator.BuildInsertQuery(conditions));

            return query.Close();
        }

        private List<IQuery> BuildConditionsDeleteQuerySingleRow(IReadOnlyList<DatabaseKey> keys, IDatabaseTableData tableData)
        {
            List<IQuery> queries = new List<IQuery>();

            if (tableData.TableDefinition.Condition == null)
                return queries;

            foreach (var entity in tableData.Entities)
            {
                if (entity.Conditions == null)
                    continue;

                int sourceGroup = 0;
                int sourceEntry = 0;
                int sourceId = 0;

                if (tableData.TableDefinition.Condition.SourceEntryColumn != null &&
                    entity.GetCell(tableData.TableDefinition.Condition.SourceEntryColumn.Value) is DatabaseField<long>
                        entryCell)
                    sourceEntry = (int)entryCell.Current.Value;

                if (tableData.TableDefinition.Condition.SourceGroupColumn != null &&
                    entity.GetCell(tableData.TableDefinition.Condition.SourceGroupColumn.Name) is DatabaseField<long>
                        groupCell)
                    sourceGroup = tableData.TableDefinition.Condition.SourceGroupColumn.Calculate((int)groupCell.Current.Value);

                if (tableData.TableDefinition.Condition.SourceIdColumn != null &&
                    entity.GetCell(tableData.TableDefinition.Condition.SourceIdColumn.Value) is DatabaseField<long>
                        idCell)
                    sourceId = (int)idCell.Current.Value;

                queries.Add(conditionQueryGenerator.BuildDeleteQuery(new IDatabaseProvider.ConditionKey(
                    tableData.TableDefinition.Condition.SourceType,
                    sourceGroup != 0 ? sourceGroup : null,
                    sourceEntry != 0 ? sourceEntry : null,
                    sourceId != 0 ? sourceId : null)));
            }

            return queries;
        }

        private IQuery BuildConditionsDeleteQuery(IReadOnlyList<DatabaseKey> keys, IDatabaseTableData tableData)
        {
            if (tableData.TableDefinition.Condition == null || keys.Count == 0)
                return Queries.Empty(tableData.TableDefinition.DataDatabaseType);

            Debug.Assert(keys[0].Count == 1);
            
            string? columnKey = null;

            bool doAbs = false;
            if (tableData.TableDefinition.Condition.SourceEntryColumn ==
                tableData.TableDefinition.TablePrimaryKeyColumnName)
                columnKey = "SourceEntry";
            else if (tableData.TableDefinition.Condition.SourceGroupColumn?.Name ==
                     tableData.TableDefinition.TablePrimaryKeyColumnName)
            {
                columnKey = "SourceGroup";
                doAbs = tableData.TableDefinition.Condition.SourceGroupColumn?.IsAbs ?? false;
            }
            else if (tableData.TableDefinition.Condition.SourceIdColumn ==
                     tableData.TableDefinition.TablePrimaryKeyColumnName)
                columnKey = "SourceId";

            if (columnKey == null)
                throw new Exception("No condition source group/entry/id is table primary key. Unable to generate SQL.");

            var distinctKeys = keys.Select(k => k[0]).Distinct();
            if (doAbs)
                distinctKeys = distinctKeys.Select(Math.Abs);

            ConditionDeleteModel model = default;
            if (columnKey == "SourceEntry")
                model = ConditionDeleteModel.ByEntry(tableData.TableDefinition.Condition.SourceType, distinctKeys.ToList());
            else if (columnKey == "SourceGroup")
                model = ConditionDeleteModel.ByGroup(tableData.TableDefinition.Condition.SourceType, distinctKeys.ToList());
            else if (columnKey == "SourceId")
                model = ConditionDeleteModel.ById(tableData.TableDefinition.Condition.SourceType,
                    distinctKeys.ToList());
            else
                throw new Exception("Invalid condition delete");
            return conditionDeleteGenerator.Delete(model)!;
        }

        public IQuery GenerateUpdateFieldQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity,
            IDatabaseField field)
        {
            var column = table.TableColumns[field.FieldName];
            var tableId = column.ForeignTable != null ? new DatabaseTable(table.DataDatabaseType, column.ForeignTable) : table.Id;
            var q = Queries.Table(tableId);
            var where = GenerateConditionsForSingleRow(q, table, tableId, entity);
            return where
                .Set(field.FieldName.ColumnName, FixUnixTimestampAndNullability(column, field.Object))
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
            IMultiQuery query = Queries.BeginTransaction(definition.DataDatabaseType);
            Dictionary<DatabaseTable, List<IDatabaseField>> fieldsByTable = entity.Fields
                .Select(ef => (ef, definition.TableColumns[ef.FieldName]))
                .Where(pair => pair.Item2.IsActualDatabaseColumn)
                .GroupBy(pair => new DatabaseTable(definition.DataDatabaseType, pair.Item2.ForeignTable ?? definition.TableName))
                .ToDictionary(g => g.Key, g => g.Select(f => f.ef).ToList());

            if (definition.ForeignTable != null)
            {
                foreach (var foreign in definition.ForeignTable)
                {
                    for (var index = foreign.ForeignKeys.Length - 1; index >= 0; index--)
                    {
                        var foreignKey = new ColumnFullName(foreign.TableName, foreign.ForeignKeys[index].ColumnName);
                        var thisKey = definition.PrimaryKey![index];
                        
                        fieldsByTable[new DatabaseTable(definition.DataDatabaseType, foreign.TableName)].Insert(0,
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
                    
                    IList<ColumnFullName> groupByKeys = definition.GroupByKeys;
                    string? autoFillBuildColumn = definition.AutofillBuildColumn;
                    if (table.Key != definition.Id)
                    {
                        var foreignTableDefinition = definition.ForeignTableByName![table.Key.Table];
                        autoFillBuildColumn = foreignTableDefinition.AutofillBuildColumn;
                        groupByKeys = foreignTableDefinition.ForeignKeys.Take(groupByKeys.Count).ToList();
                        query.Table(table.Key)
                            .InsertIgnore(
                                definition.ForeignTableByName[table.Key.Table].ForeignKeys
                                    .Zip(definition.PrimaryKey.Select(x => new ColumnFullName(null, x.ColumnName)))
                                    .ToDictionary<(ColumnFullName, ColumnFullName), string, object?>(
                                    key => key.Item1.ColumnName,
                                    key => entity.GetTypedValueOrThrow<long>(key.Item2))
                                );
                    }

                    var where = GenerateWherePrimaryKey(groupByKeys, query.Table(table.Key), entity.Key);
                    IUpdateQuery update = where
                        .Set(updates[0].FieldName.ColumnName, FixUnixTimestampAndNullability(definition.TableColumns[updates[0].FieldName], updates[0].Object));
                    for (int i = 1; i < updates.Count; ++i)
                        update = update.Set(updates[i].FieldName.ColumnName, FixUnixTimestampAndNullability(definition.TableColumns[updates[i].FieldName], updates[i].Object));

                    if (autoFillBuildColumn != null)
                        update = update.Set(autoFillBuildColumn, currentCoreVersion.Current.Version.Build);
                    
                    update.Update();
                }

                if (entity.ConditionsModified)
                {
                    query.Add(BuildConditions(new []{entity.Key}, new DatabaseTableData(definition, new[]{entity})));
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
                    if (table.Key == definition.Id)
                    {
                        var cells = table.Value.ToDictionary(t => t.FieldName.ColumnName, t => t.Object);
                        if (definition.AutofillBuildColumn is {} autofillBuildColumn)
                            cells[autofillBuildColumn] = currentCoreVersion.Current.Version.Build;
                        query.Table(table.Key)
                            .Insert(cells);
                    }
                    else
                    {
                        var isModified = table.Value.Any(f => f.IsModified);
                        if (isModified)
                        {
                            var updates = table.Value
                                .Where(f => f.IsModified)
                                .ToList();
                            var foreignTableDefinition = definition.ForeignTableByName![table.Key.Table];
                            var primaryKeyColumn = foreignTableDefinition.ForeignKeys.Take(definition.GroupByKeys.Count).ToList();
                            query.Table(table.Key)
                                .InsertIgnore(
                                    definition.ForeignTableByName[table.Key.Table].ForeignKeys
                                        .Zip(definition.PrimaryKey.Select(k => new ColumnFullName(null, k.ColumnName)))
                                        .ToDictionary<(ColumnFullName, ColumnFullName), string, object?>(
                                            key => key.Item1.ColumnName,
                                            key => entity.GetTypedValueOrThrow<long>(key.Item2))
                                );
                            var where = GenerateWherePrimaryKey(primaryKeyColumn, query.Table(table.Key), entity.GenerateKey(definition));
                            IUpdateQuery update = where
                                .Set(updates[0].FieldName.ColumnName, FixUnixTimestampAndNullability(definition.TableColumns[updates[0].FieldName], updates[0].Object));
                            for (int i = 1; i < updates.Count; ++i)
                                update = update.Set(updates[i].FieldName.ColumnName, FixUnixTimestampAndNullability(definition.TableColumns[updates[i].FieldName], updates[i].Object));

                            if (foreignTableDefinition.AutofillBuildColumn is { } autofillBuildColumn)
                                update = update.Set(autofillBuildColumn, currentCoreVersion.Current.Version.Build);

                            update.Update();
                        }
                    }
                }
                query.Add(BuildConditions(new []{entity.Key}, new DatabaseTableData(definition, new[]{entity})));
            }

            return query.Close();
        }
        
        private IQuery GenerateUpdateQuery(IDatabaseTableData tableData)
        {
            IMultiQuery query = Queries.BeginTransaction(tableData.TableDefinition.DataDatabaseType);
            
            foreach (var entity in tableData.Entities)
            {
                query.Add(GenerateUpdateQuery(tableData.TableDefinition, entity));
            }

            return query.Close();
        }

        private IWhere GenerateConditionsForSingleRow(ITable table, DatabaseTableDefinitionJson definition, DatabaseTable tableName, DatabaseEntity entity)
        {
            // todo: this might not be good after change to DatabaseKeys
            if (tableName == definition.Id)
            {
                return GenerateWherePrimaryKey(definition, table, entity.GenerateKey(definition));
            } 
            else
            {
                var foreignKeys = definition.ForeignTableByName![tableName.Table].ForeignKeys;

                var where = table
                    .Where(row =>
                        row.Column<uint>(foreignKeys[0].ColumnName) ==
                        (uint)entity.GetTypedValueOrThrow<long>(definition.PrimaryKey![0]));
                            
                for (int i = 1; i < foreignKeys.Length; ++i)
                {
                    var foreignKey = foreignKeys[i];
                    var thisKey = definition.PrimaryKey![i];
                    where = where.Where(row =>
                        row.Column<uint>(foreignKey.ColumnName) == (uint)entity.GetTypedValueOrThrow<long>(thisKey));
                }

                return where;
            }
        }
    }
}