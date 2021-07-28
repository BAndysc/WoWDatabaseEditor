using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Expressions;
using WDE.DatabaseEditors.Extensions;
using WDE.DatabaseEditors.Models;
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
        
        public IQuery GenerateQuery(ICollection<uint> keys, IDatabaseTableData tableData)
        {
            if (tableData.TableDefinition.IsOnlyConditionsTable)
                return BuildConditions(keys, tableData);
            if (tableData.TableDefinition.IsMultiRecord)
                return GenerateInsertQuery(keys, tableData);
            return GenerateUpdateQuery(tableData);
        }

        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity)
        {
            return Queries
                .Table(table.TableName)
                .Where(r => r.Column<uint>(table.TablePrimaryKeyColumnName) == entity.Key)
                .Delete();
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

                if (definition.SortBy == null || definition.SortBy.Length == 0)
                    return 0;

                foreach (var sortBy in definition.SortBy)
                {
                    var comparisonResult = x.GetCell(sortBy)?.CompareTo(y.GetCell(sortBy)) ?? 0;
                    if (comparisonResult != 0) 
                        return comparisonResult;
                }
                
                var existInDatabaseComparison = x.ExistInDatabase.CompareTo(y.ExistInDatabase);
                if (existInDatabaseComparison != 0) return existInDatabaseComparison;
                return x.Key.CompareTo(y.Key);
            }
        }

        private IQuery GenerateInsertQuery(ICollection<uint> keys, IDatabaseTableData tableData)
        {
            if (keys.Count == 0)
                return Queries.Empty();

            IMultiQuery query = Queries.BeginTransaction();
            query
                .Table(tableData.TableDefinition.TableName)
                .WhereIn(tableData.TableDefinition.TablePrimaryKeyColumnName, keys.Distinct())
                .Delete();

            if (tableData.Entities.Count == 0)
                return query.Close();
            
            var columns = tableData.TableDefinition.TableColumns
                .Select(c => c.Value)
                .Where(col => !col.IsMetaColumn && !col.IsConditionColumn)
                .ToList();

            HashSet<EntityKey> entityKeys = new();
            List<Dictionary<string, object?>> inserts = new(tableData.Entities.Count);
            List<string> duplicates = new List<string>();
            var comparer = new EntityComparer(tableData.TableDefinition);
            foreach (var entity in tableData.Entities.OrderBy(t => t, comparer))
            {
                bool duplicate = tableData.TableDefinition.PrimaryKey != null && !entityKeys.Add(new EntityKey(entity, tableData.TableDefinition));
                var cells = columns.ToDictionary(c => c.DbColumnName, c =>
                {
                    var cell = entity.GetCell(c.DbColumnName)!;
                    if (c.AutogenerateComment != null && cell is DatabaseField<string> sField)
                    {
                        var evaluator = new DatabaseExpressionEvaluator(calculatorService, parameterFactory, tableData.TableDefinition, c.AutogenerateComment!);
                        var comment = evaluator.Evaluate(entity);
                        if (comment is string s)
                            return s.AddComment(sField.Current.Value);
                    }
                    return cell.Object;
                });
                
                if (duplicate)
                    duplicates.Add("(" + string.Join(", ", cells.Values) + ")");
                else
                    inserts.Add(cells);
            }

            query.Table(tableData.TableDefinition.TableName)
                .BulkInsert(inserts);
            
            if (duplicates.Count > 0)
            {
                query.Comment("duplicates, cannot insert:");
                foreach (var line in duplicates)
                    query.Comment(line);
            }

            query.Add(BuildConditions(keys, tableData));

            return query.Close();
        }

        private IQuery BuildConditions(ICollection<uint> keys, IDatabaseTableData tableData)
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

        private IQuery BuildConditionsDeleteQuery(ICollection<uint> keys, IDatabaseTableData tableData)
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
            string primaryKeyColumn = table.TablePrimaryKeyColumnName;
            if (column.ForeignTable != null)
                primaryKeyColumn = table.ForeignTableByName[column.ForeignTable].ForeignKey;

            return Queries
                .Table(table.TableName)
                .Where(row => row.Column<uint>(primaryKeyColumn) == entity.Key)
                .Set(field.FieldName, field.Object)
                .Update();
        }

        private IQuery GenerateUpdateQuery(IDatabaseTableData tableData)
        {
            IMultiQuery query = Queries.BeginTransaction();
            
            foreach (var entity in tableData.Entities)
            {
                Dictionary<string, List<IDatabaseField>> fieldsByTable = entity.Fields
                    .Select(ef => (ef, tableData.TableDefinition.TableColumns[ef.FieldName]))
                    .Where(pair => !pair.Item2.IsMetaColumn && !pair.Item2.IsConditionColumn)
                    .GroupBy(pair => pair.Item2.ForeignTable ?? tableData.TableDefinition.TableName)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ef).ToList());

                if (tableData.TableDefinition.ForeignTable != null)
                {
                    foreach (var foreign in tableData.TableDefinition.ForeignTable)
                        fieldsByTable[foreign.TableName].Insert(0, new DatabaseField<long>(foreign.ForeignKey, new ValueHolder<long>(entity.Key, false)));
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
                        
                        string primaryKeyColumn = tableData.TableDefinition.TablePrimaryKeyColumnName;
                        if (table.Key != tableData.TableDefinition.TableName)
                        {
                            primaryKeyColumn = tableData.TableDefinition.ForeignTableByName[table.Key].ForeignKey;
                            query.Table(table.Key)
                                .InsertIgnore(new Dictionary<string, object?>(){{primaryKeyColumn, entity.Key}});
                        }

                        IUpdateQuery update = query.Table(table.Key)
                            .Where(row => row.Column<uint>(primaryKeyColumn) == entity.Key)
                            .Set(updates[0].FieldName, updates[0].Object);
                        for (int i = 1; i < updates.Count; ++i)
                            update = update.Set(updates[i].FieldName, updates[i].Object);

                        update.Update();
                    }
                }
                else
                {
                    foreach (var table in fieldsByTable)
                    {
                        string primaryKeyColumn = tableData.TableDefinition.TablePrimaryKeyColumnName;
                        if (table.Key != tableData.TableDefinition.TableName)
                            primaryKeyColumn = tableData.TableDefinition.ForeignTableByName[table.Key].ForeignKey;

                        query.Table(table.Key)
                            .Where(row => row.Column<uint>(primaryKeyColumn) == entity.Key)
                            .Delete();

                        query.Table(table.Key)
                            .Insert(table.Value.ToDictionary(t => t.FieldName, t => t.Object));
                    }
                }
            }

            return query.Close();
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