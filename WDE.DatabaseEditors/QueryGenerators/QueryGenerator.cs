using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WDE.Common.Database;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.QueryGenerators
{
    [SingleInstance]
    [AutoRegister]
    public class QueryGenerator : IQueryGenerator
    {       
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly IConditionQueryGenerator conditionQueryGenerator;

        public QueryGenerator(ITableDefinitionProvider tableDefinitionProvider, IConditionQueryGenerator conditionQueryGenerator)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.conditionQueryGenerator = conditionQueryGenerator;
        }
        
        public string GenerateQuery(ICollection<uint> keys, IDatabaseTableData tableData)
        {
            if (tableData.TableDefinition.IsMultiRecord)
                return GenerateInsertQuery(keys, tableData);
            return GenerateUpdateQuery(tableData);
        }

        public string GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity)
        {
            return $"DELETE FROM {table.TableName} WHERE {table.TablePrimaryKeyColumnName} = {entity.Key};";
        }
 
        private string GenerateInsertQuery(ICollection<uint> keys, IDatabaseTableData tableData)
        {
            if (keys.Count == 0)
                return "";

            StringBuilder query = new();
            var keysString = string.Join(", ",  keys.Distinct());

            query.AppendLine(
                $"DELETE FROM {tableData.TableDefinition.TableName} WHERE {tableData.TableDefinition.TablePrimaryKeyColumnName} IN ({keysString});");

            if (tableData.Entities.Count == 0)
                return query.ToString();


            var columns = tableData.TableDefinition.TableColumns
                .Select(c => c.Value)
                .Where(col => !col.IsMetaColumn && !col.IsConditionColumn)
                .ToList();
            var columnsString = string.Join(", ", columns.Select(f => $"`{f.DbColumnName}`"));

            query.AppendLine($"INSERT INTO {tableData.TableDefinition.TableName} ({columnsString}) VALUES");

            HashSet<EntityKey> entityKeys = new();
            List<string> inserts = new List<string>(tableData.Entities.Count);
            List<string> duplicates = new List<string>();
            foreach (var entity in tableData.Entities)
            {
                bool duplicate = tableData.TableDefinition.PrimaryKey != null && !entityKeys.Add(new EntityKey(entity, tableData.TableDefinition));
                var cells = columns.Select(c => entity.GetCell(c.DbColumnName)!.ToQueryString());
                var cellStrings = string.Join(", ", cells);
                
                if (duplicate)
                    duplicates.Add($"({cellStrings})");
                else
                    inserts.Add($"({cellStrings})");
            }

            query.Append(string.Join(",\n", inserts));
            query.AppendLine(";");

            if (duplicates.Count > 0)
            {
                query.AppendLine(" -- duplicates, cannot insert:");
                foreach (var line in duplicates)
                    query.AppendLine(" -- " + line);
            }

            if (tableData.TableDefinition.Condition != null)
            {
                query.AppendLine(BuildConditionsDeleteQuery(keys, tableData));
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
                        sourceEntry = (int)entryCell.Current.Value;
                    
                    if (tableData.TableDefinition.Condition.SourceGroupColumn != null &&
                        entity.GetCell(tableData.TableDefinition.Condition.SourceGroupColumn) is DatabaseField<long>
                            groupCell)
                        sourceGroup = (int)groupCell.Current.Value;
                    
                    if (tableData.TableDefinition.Condition.SourceIdColumn != null &&
                        entity.GetCell(tableData.TableDefinition.Condition.SourceIdColumn) is DatabaseField<long>
                            idCell)
                        sourceId = (int)idCell.Current.Value;
                    
                    foreach (var condition in entity.Conditions)
                        conditions.Add(new AbstractConditionLine(sourceType, sourceGroup, sourceEntry, sourceId, condition));
                }
                query.AppendLine(conditionQueryGenerator.BuildInsertQuery(conditions));
            }

            return query.ToString();
        }

        private string BuildConditionsDeleteQuery(ICollection<uint> keys, IDatabaseTableData tableData)
        {
            if (tableData.TableDefinition.Condition == null)
                return "";

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
            
            string c = string.Join(", ", keys.Distinct());
            return $"DELETE FROM `conditions` WHERE `SourceTypeOrReferenceId` = {tableData.TableDefinition.Condition.SourceType} AND `{columnKey}` IN ({c});";
        }

        public string GenerateUpdateFieldQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity,
            IDatabaseField field)
        {
            var column = table.TableColumns[field.FieldName];
            string primaryKeyColumn = table.TablePrimaryKeyColumnName;
            if (column.ForeignTable != null)
                primaryKeyColumn = table.ForeignTableByName[column.ForeignTable].ForeignKey;
            
            return
                $"UPDATE {table.TableName} SET `{field.FieldName}` = {field.ToQueryString()} WHERE `{primaryKeyColumn}` = {entity.Key};";
        }

        private string GenerateUpdateQuery(IDatabaseTableData tableData)
        {
            StringBuilder query = new();
            
            foreach (var entity in tableData.Entities)
            {
                Dictionary<string, List<IDatabaseField>> fieldsByTable = null!;
                fieldsByTable = entity.Fields
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
                        var updates = string.Join(", ",
                            table.Value
                                .Where(f => f.IsModified)
                                .Select(f => $"`{f.FieldName}` = {f.ToQueryString()}"));
                
                        if (string.IsNullOrEmpty(updates))
                            continue;

                        string primaryKeyColumn = tableData.TableDefinition.TablePrimaryKeyColumnName;
                        if (table.Key != tableData.TableDefinition.TableName)
                        {
                            primaryKeyColumn = tableData.TableDefinition.ForeignTableByName[table.Key].ForeignKey;
                            query.AppendLine(
                                $"INSERT IGNORE INTO {table.Key} (`{primaryKeyColumn}`) VALUES ({entity.Key});");
                        }
                        
                        var updateQuery = $"UPDATE `{table.Key}` SET {updates} WHERE `{primaryKeyColumn}`= {entity.Key};";
                        query.AppendLine(updateQuery);
                    }
                }
                else
                {
                    foreach (var table in fieldsByTable)
                    {
                        string primaryKeyColumn = tableData.TableDefinition.TablePrimaryKeyColumnName;
                        if (table.Key != tableData.TableDefinition.TableName)
                            primaryKeyColumn = tableData.TableDefinition.ForeignTableByName[table.Key].ForeignKey;
                        
                        query.AppendLine(
                            $"DELETE FROM {table.Key} WHERE `{primaryKeyColumn}` = {entity.Key};");
                        var columns = string.Join(", ", table.Value.Select(f => $"`{f.FieldName}`"));
                        query.AppendLine($"INSERT INTO {table.Key} ({columns}) VALUES");
                        var values = string.Join(", ", table.Value.Select(f => f.ToQueryString()));
                        query.AppendLine($"({values});");
                    }
                }
            }

            return query.ToString();
        }

        private class EntityKey
        {
            private IList<IDatabaseField> fields;
            private int hash;
            
            public EntityKey(DatabaseEntity entity, DatabaseTableDefinitionJson table)
            {
                fields = table.PrimaryKey!.Select(key => entity.GetCell(key)!).ToList();
                hash = 0;
                foreach (var field in fields)
                    hash = HashCode.Combine(hash, field.GetHashCode());
            }

            protected bool Equals(EntityKey other)
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