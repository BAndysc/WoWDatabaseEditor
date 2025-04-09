using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Exceptions;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Loaders
{
    [SingleInstance]
    [AutoRegister]
    public class DatabaseTableDataProvider : IDatabaseTableDataProvider
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly IDatabaseQueryExecutor sqlExecutor;
        private readonly IMessageBoxService messageBoxService;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IDatabaseTableModelGenerator tableModelGenerator;
        private readonly Dictionary<DatabaseTable, ICustomDatabaseTableSourcePostLoad> customPostLoadSources;

        public DatabaseTableDataProvider(ITableDefinitionProvider tableDefinitionProvider,
            IDatabaseQueryExecutor sqlExecutor,
            IMessageBoxService messageBoxService,
            IDatabaseProvider databaseProvider,
            IDatabaseTableModelGenerator tableModelGenerator,
            IEnumerable<ICustomDatabaseTableSourcePostLoad> customPostLoadSources)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.sqlExecutor = sqlExecutor;
            this.messageBoxService = messageBoxService;
            this.databaseProvider = databaseProvider;
            this.tableModelGenerator = tableModelGenerator;
            this.customPostLoadSources = customPostLoadSources.ToDictionary(x => x.Table, x => x);
        }

        private (string query, List<ColumnFullName> selectedColumns) BuildSQLQueryForSingleRow(DatabaseTableDefinitionJson tableDefinitionJson, string? customWhere, long? offset, int? limit)
        {
            var tableName = tableDefinitionJson.TableName;
            var columns = tableDefinitionJson.Groups
                .SelectMany(x => x.Fields)
                .Where(x => x.IsActualDatabaseColumn)
                .Select(x =>
                {
                    var column =  $"`{x.ForeignTable ?? tableName}`.`{x.DbColumnName}`";
                    if (x.IsUnixTimestamp)
                        column = $"UNIX_TIMESTAMP({column}) AS `{x.DbColumnName}`";
                    return (column, x.DbColumnFullName);
                })
                .Distinct()
                .ToList();
            var names = string.Join(",", columns.Select(x => x.column));
            var joins = "";

            if (tableDefinitionJson.ForeignTable != null)
            {
                joins += string.Join(" ", tableDefinitionJson.ForeignTable.Select(table =>
                {
                    var where = table.ForeignKeys.Zip(tableDefinitionJson.PrimaryKey!)
                        .Select(pair => $"`{table.TableName}`.`{pair.First}` = `{tableName}`.`{pair.Second}`");
                    return $"LEFT JOIN `{table.TableName}` ON " + string.Join(" AND ", where);
                }));
            }

            var where = string.IsNullOrEmpty(customWhere) ? "" : $"WHERE ({customWhere})";
            var query = $"SELECT {names} FROM `{tableDefinitionJson.TableName}` {joins} {where} ORDER BY {string.Join(", ", tableDefinitionJson.PrimaryKey.Select(x => $"`{x}`"))} ASC LIMIT {limit ?? 300} OFFSET {offset ?? 0}";
            return (query, columns.Select(c => c.DbColumnFullName).ToList());
        }

        private (string query, List<ColumnFullName> selectedColumns) BuildSQLQueryFromTableDefinition(DatabaseTableDefinitionJson tableDefinitionJson, DatabaseKey[] entries)
        {
            var tableName = tableDefinitionJson.TableName;
            var tablePrimaryKey = tableDefinitionJson.TablePrimaryKeyColumnName;
            var columns = tableDefinitionJson.Groups
                .SelectMany(x => x.Fields)
                .Where(x => x.IsActualDatabaseColumn)
                .Select(x =>
                {
                    var column =  $"`{x.ForeignTable ?? tableName}`.`{x.DbColumnName}`";
                    if (x.IsUnixTimestamp)
                        column = $"UNIX_TIMESTAMP({column}) AS `{x.DbColumnName}`";
                    return (column, x.DbColumnFullName);
                })
                .Distinct()
                .ToList();
            var names = string.Join(",", columns.Select(x => x.column));
            var joins = "";

            if (tableDefinitionJson.ForeignTable != null)
            {
                joins += string.Join(" ", tableDefinitionJson.ForeignTable.Select(table =>
                {
                    var where = table.ForeignKeys.Zip(tableDefinitionJson.PrimaryKey!)
                        .Select(pair => $"`{table.TableName}`.`{pair.First}` = `{tableName}`.`{pair.Second}`");
                    return $"LEFT JOIN `{table.TableName}` ON " + string.Join(" AND ", where);
                }));
            }

            var query = $"SELECT {names} FROM {tableDefinitionJson.TableName} {joins} WHERE `{tableName}`.`{tablePrimaryKey}` IN ({string.Join(", ", entries)});";
            return (query, columns.Select(c => c.DbColumnFullName).ToList());
        }

        public async Task<long> GetCount(DatabaseTable tableName, string? customWhere, IEnumerable<DatabaseKey>? keys)
        {
            var definition = tableDefinitionProvider.GetDefinitionByTableName(tableName);
            if (definition == null)
                return 0;

            if (keys != null)
            {
                var whereKeys = BuildWhereFromKeys(definition, keys.ToArray());
                if (string.IsNullOrEmpty(customWhere))
                    customWhere = whereKeys;
                else
                    customWhere = "(" + customWhere + ") AND " + whereKeys;
            }

            var where = string.IsNullOrEmpty(customWhere) ? "" : $"WHERE ({customWhere})";
            var sql = $"SELECT COUNT(*) AS num FROM {definition.TableName} {where}";
            try
            {
                var result = await sqlExecutor.ExecuteSelectSql(definition, sql);
                if (result.Rows == 0)
                    return 0;
                return Convert.ToInt64(result.Value(0, 0));
            }

            catch (IMySqlExecutor.CannotConnectToDatabaseException)
            {
                return 0;
            }
            catch (Exception e)
            {
                LOG.LogError(e.Message);
                return 0;
            }
        }

        private string BuildWhereFromKeys(DatabaseTableDefinitionJson definition, DatabaseKey[] keys)
        {
            if (keys.Length == 0)
                return "false";
            if (definition.GroupByKeys.Count == 1)
            {
                return $"`{definition.TableName}`.`{definition.GroupByKeys[0]}` IN ({string.Join(", ", keys.Select(k => k[0]))})";
            }
            else
            {
                Debug.Assert(keys[0].Count == definition.GroupByKeys.Count);
                return string.Join(" OR ", keys.Select(key =>
                {
                    StringBuilder sb = new();
                    for (int i = 0; i < definition.GroupByKeys.Count; ++i)
                    {
                        if (i != 0)
                            sb.Append(" AND ");
                        sb.Append($"(`{definition.TableName}`.`{definition.GroupByKeys[i]}` = {key[i]})");
                    }
                    return sb.ToString();
                }));
            }
        }

        public async Task<IDatabaseTableData?> Load(DatabaseTable tableName, string? customWhere, long? offset, int? limit, DatabaseKey[]? keys)
        {
            var definition = tableDefinitionProvider.GetDefinitionByTableName(tableName);
            if (definition == null)
                return null;

            IReadOnlyList<ColumnFullName> columns = Array.Empty<ColumnFullName>();
            IDatabaseSelectResult? result = null;
            IDatabaseProvider.ConditionKeyMask keyMask = IDatabaseProvider.ConditionKeyMask.None;
            IList<IConditionLine>[]? conditionsPerRowIndex = null;
            if (definition.Condition != null)
            {
                if (definition.Condition.SourceEntryColumn != null)
                    keyMask |= IDatabaseProvider.ConditionKeyMask.SourceEntry;
                if (definition.Condition.SourceGroupColumn != null)
                    keyMask |= IDatabaseProvider.ConditionKeyMask.SourceGroup;
                if (definition.Condition.SourceIdColumn != null)
                    keyMask |= IDatabaseProvider.ConditionKeyMask.SourceId;
            }

            if (definition.RecordMode == RecordMode.SingleRow)
            {
                if (keys != null)
                {
                    var whereKeys = BuildWhereFromKeys(definition, keys);
                    if (string.IsNullOrEmpty(customWhere))
                        customWhere = whereKeys;
                    else
                        customWhere = "(" + customWhere + ") AND " + whereKeys;
                }
                var (sqlStatement, selectedColumns) = BuildSQLQueryForSingleRow(definition, customWhere, offset, Math.Min(limit ?? 300, 3000));
                columns = selectedColumns;
                try
                {
                    result = await sqlExecutor.ExecuteSelectSql(definition, sqlStatement);

                    if (result.Columns > 0 && definition.Condition != null)
                    {
                        conditionsPerRowIndex = new IList<IConditionLine>[result.Rows];

                        List<(int rowIndex, long? sourceGroup, int? sourceEntry, int? sourceId)> conditionsToLoad = new();
                        var sourceGroupColumnIndex = definition.Condition.SourceGroupColumn == null ? null : (int?)result.ColumnIndex(definition.Condition.SourceGroupColumn.Name.ColumnName);
                        var sourceEntryColumnIndex = definition.Condition.SourceEntryColumn == null ? null : (int?)result.ColumnIndex(definition.Condition.SourceEntryColumn.Value.ColumnName);
                        var sourceIdColumnIndex = definition.Condition.SourceIdColumn == null ? null : (int?) result.ColumnIndex(definition.Condition.SourceIdColumn.Value.ColumnName);
                        foreach (var row in result)
                        {
                            long? sourceGroup = null;
                            int? sourceEntry = null, sourceId = null;

                            if (sourceGroupColumnIndex.HasValue &&
                                int.TryParse(result.Value(row, sourceGroupColumnIndex.Value)!.ToString(), out var groupLong))
                                sourceGroup = definition.Condition.SourceGroupColumn!.Calculate(groupLong);

                            if (sourceEntryColumnIndex.HasValue &&
                                int.TryParse(result.Value(row, sourceEntryColumnIndex.Value)!.ToString(), out var entryInt))
                                sourceEntry = entryInt;

                            if (sourceIdColumnIndex.HasValue &&
                                int.TryParse(result.Value(row, sourceIdColumnIndex.Value)!.ToString(), out var idInt))
                                sourceId = idInt;

                            conditionsToLoad.Add((row, sourceGroup, sourceEntry, sourceId));
                            conditionsPerRowIndex[row] = new List<IConditionLine>();
                        }

                        var allConditions = await databaseProvider.GetConditionsForAsync(keyMask, conditionsToLoad.Select(c =>
                            new IDatabaseProvider.ConditionKey(definition.Condition.SourceType, c.sourceGroup, c.sourceEntry, c.sourceId)).ToList());

                        var groupedConditions = allConditions.GroupBy(line => (keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceGroup) ? (long?)line.SourceGroup : null,
                            keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceEntry) ? (int?)line.SourceEntry : null,
                            keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceId) ? (int?)line.SourceId : null))
                            .ToDictionary(key => key.Key, values => values.ToList());

                        foreach (var (rowIndex, sourceGroup, sourceEntry, sourceId) in conditionsToLoad)
                        {
                            if (groupedConditions.TryGetValue((sourceGroup, sourceEntry, sourceId), out var conditionList))
                            {
                                conditionsPerRowIndex[rowIndex] = conditionList;
                            }
                        }
                    }
                }
                catch (IMySqlExecutor.CannotConnectToDatabaseException)
                {
                }
                catch (IMySqlExecutor.QueryFailedDatabaseException e)
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Query error")
                        .SetMainInstruction(
                            "Unable to execute SQL query.")
                        .SetContent(e.Message)
                        .SetIcon(MessageBoxIcon.Error)
                        .WithOkButton(false)
                        .Build());
                }
                catch (Exception e)
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Database error")
                        .SetMainInstruction(
                            "Unable to execute SQL query. Most likely your database is incompatible with provided database schema, if you think this is a bug, report it via Help -> Report Bug")
                        .SetContent(e.ToString())
                        .SetIcon(MessageBoxIcon.Error)
                        .WithOkButton(false)
                        .Build());
                    return null;
                }
            }
            else if (keys != null && keys.Length > 0)
            {
                Debug.Assert(customWhere == null, "Custom where with non single record mode is not supported");
                if (definition.IsOnlyConditionsTable == OnlyConditionMode.IgnoreTableCompletely)
                {
                    if (definition.Condition == null)
                        throw new Exception("only_conditions + no conditions make no sense");

                    var conditionsResult = new ConditionsOnlyDatabaseSource(definition.Condition.SourceGroupColumn?.Name.ColumnName, definition.Condition.SourceEntryColumn?.ColumnName, definition.Condition.SourceIdColumn?.ColumnName);
                    result = conditionsResult;
                    var fakeColumns = new List<ColumnFullName>();
                    if (definition.Condition.SourceGroupColumn != null)
                        fakeColumns.Add(definition.Condition.SourceGroupColumn.Name);
                    if (definition.Condition.SourceEntryColumn != null)
                        fakeColumns.Add(definition.Condition.SourceEntryColumn.Value);
                    if (definition.Condition.SourceIdColumn != null)
                        fakeColumns.Add(definition.Condition.SourceIdColumn.Value);
                    columns = fakeColumns;

                    foreach (var key in keys)
                    {
                        Debug.Assert(key.Count == 1, "todo?");
                        long? sourceGroup = null;
                        int? sourceEntry = null, sourceId = null;
                        if (definition.Condition.SourceGroupColumn != null &&
                            definition.TablePrimaryKeyColumnName == definition.Condition.SourceGroupColumn.Name)
                        {
                            keyMask = IDatabaseProvider.ConditionKeyMask.SourceGroup;
                            sourceGroup = definition.Condition.SourceGroupColumn.Calculate(key[0]);
                        }

                        if (definition.Condition.SourceEntryColumn != null &&
                            definition.TablePrimaryKeyColumnName == definition.Condition.SourceEntryColumn)
                        {
                            keyMask = IDatabaseProvider.ConditionKeyMask.SourceEntry;
                            sourceEntry = (int)key[0];
                        }

                        if (definition.Condition.SourceIdColumn != null &&
                            definition.TablePrimaryKeyColumnName == definition.Condition.SourceIdColumn)
                        {
                            keyMask = IDatabaseProvider.ConditionKeyMask.SourceId;
                            sourceId = (int)key[0];
                        }

                        IReadOnlyList<IConditionLine>? conditionList = await databaseProvider.GetConditionsForAsync(keyMask,
                            new IDatabaseProvider.ConditionKey(definition.Condition.SourceType, sourceGroup,
                                sourceEntry, sourceId));
                        if (conditionList != null && conditionList.Count > 0)
                        {
                            foreach (var distinct in conditionList
                                .Select(line => (line.SourceEntry, line.SourceGroup, line.SourceId)).Distinct())
                            {
                                var conditions = conditionList.Where(l =>
                                    l.SourceEntry == distinct.SourceEntry && l.SourceId == distinct.SourceId &&
                                                    l.SourceGroup == distinct.SourceGroup).ToList();

                                conditionsResult.Add(distinct.SourceGroup, distinct.SourceEntry, distinct.SourceId, conditions);
                            }
                        }
                    }
                    conditionsPerRowIndex = conditionsResult.Conditions;
                }
                else
                {
                    Debug.Assert(customWhere == null, "Custom where with non single record mode is not supported");
                    var (sqlStatement, selectedColumns) = BuildSQLQueryFromTableDefinition(definition, keys);
                    columns = selectedColumns;
                    try
                    {
                        result = await sqlExecutor.ExecuteSelectSql(definition, sqlStatement);

                        if (result.Columns > 0 && definition.Condition != null)
                        {
                            conditionsPerRowIndex = new IList<IConditionLine>[result.Rows];
                            List<(int rowIndex, long? sourceGroup, int? sourceEntry, int? sourceId)> conditionsToLoad = new();
                            var sourceGroupColumnIndex = definition.Condition.SourceGroupColumn == null ? null : (int?)result.ColumnIndex(definition.Condition.SourceGroupColumn.Name.ColumnName);
                            var sourceEntryColumnIndex = definition.Condition.SourceEntryColumn == null ? null : (int?)result.ColumnIndex(definition.Condition.SourceEntryColumn.Value.ColumnName);
                            var sourceIdColumnIndex = definition.Condition.SourceIdColumn == null ? null : (int?) result.ColumnIndex(definition.Condition.SourceIdColumn.Value.ColumnName);
                            foreach (var rowIndex in result)
                            {
                                long? sourceGroup = null;
                                int? sourceEntry = null, sourceId = null;

                                if (sourceGroupColumnIndex.HasValue &&
                                    int.TryParse(result.Value(rowIndex, sourceGroupColumnIndex.Value)!.ToString(), out var groupLong))
                                    sourceGroup = definition.Condition.SourceGroupColumn!.Calculate(groupLong);

                                if (sourceEntryColumnIndex.HasValue &&
                                    int.TryParse(result.Value(rowIndex, sourceEntryColumnIndex.Value)!.ToString(), out var entryInt))
                                    sourceEntry = entryInt;

                                if (sourceIdColumnIndex.HasValue &&
                                    int.TryParse(result.Value(rowIndex, sourceIdColumnIndex.Value)!.ToString(), out var idInt))
                                    sourceId = idInt;

                                conditionsToLoad.Add((rowIndex, sourceGroup, sourceEntry, sourceId));
                                conditionsPerRowIndex[rowIndex] = new List<IConditionLine>();
                            }

                            var allConditions = await databaseProvider.GetConditionsForAsync(keyMask, conditionsToLoad.Select(c =>
                                new IDatabaseProvider.ConditionKey(definition.Condition.SourceType, c.sourceGroup, c.sourceEntry, c.sourceId)).ToList());

                            var groupedConditions = allConditions.GroupBy(line => (keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceGroup) ? (long?)line.SourceGroup : null,
                                    keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceEntry) ? (int?)line.SourceEntry : null,
                                    keyMask.HasFlagFast(IDatabaseProvider.ConditionKeyMask.SourceId) ? (int?)line.SourceId : null))
                                .ToDictionary(key => key.Key, values => values.ToList());

                            foreach (var (rowIndex, sourceGroup, sourceEntry, sourceId) in conditionsToLoad)
                            {
                                if (groupedConditions.TryGetValue((sourceGroup, sourceEntry, sourceId), out var conditionList))
                                    conditionsPerRowIndex[rowIndex] = conditionList;
                            }
                        }
                    }
                    catch (IMySqlExecutor.CannotConnectToDatabaseException)
                    {
                    }
                    catch (Exception e)
                    {
                        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Database error")
                            .SetMainInstruction(
                                "Unable to execute SQL query. Most likely your database is incompatible with provided database schema, if you think this is a bug, report it via Help -> Report Bug")
                            .SetContent(e.ToString())
                            .SetIcon(MessageBoxIcon.Error)
                            .WithOkButton(false)
                            .Build());
                        return null;
                    }
                }
            }

            if (result == null)
                result = new EmptyDatabaseSelectResult();

            var databaseTable = tableModelGenerator.CreateDatabaseTable(definition, keys, result, columns, conditionsPerRowIndex);

            if (databaseTable != null && customPostLoadSources.TryGetValue(tableName, out var postProcessor))
                await postProcessor.PostProcess(databaseTable, definition, keys);

            return databaseTable;
        }

        private class ConditionsOnlyDatabaseSource : IDatabaseSelectResult
        {
            private readonly string? group;
            private readonly string? entry;
            private readonly string? id;
            private readonly List<string> columns;
            private List<(long col1, int col2, int col3)> rows = new();
            private List<IList<IConditionLine>> conditions = new();

            public ConditionsOnlyDatabaseSource(string? group, string? entry, string? id)
            {
                this.group = group;
                this.entry = entry;
                this.id = id;
                columns = new List<string>();
                if (group != null)
                    columns.Add(group);
                if (entry != null)
                    columns.Add(entry);
                if (id != null)
                    columns.Add(id);
            }

            public IEnumerator<int> GetEnumerator()
            {
                for (int i = 0; i < rows.Count; ++i)
                    yield return i;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Columns => columns.Count;

            public int Rows => rows.Count;

            public string ColumnName(int index) => columns[index];

            public Type ColumnType(int index) => typeof(long);

            public object? Value(int row, int column) => Value<long>(row, column);

            public T? Value<T>(int row, int column)
            {
                if (typeof(T) == typeof(int))
                {
                    if (column == 0)
                        return (T)(object)(int)(rows[row].col1);
                    if (column == 1)
                        return (T)(object)(int)(rows[row].col2);
                    if (column == 2)
                        return (T)(object)(int)(rows[row].col3);
                }
                else if (typeof(T) == typeof(long))
                {
                    if (column == 0)
                        return (T)(object)(long)(rows[row].col1);
                    if (column == 1)
                        return (T)(object)(long)(rows[row].col2);
                    if (column == 2)
                        return (T)(object)(long)(rows[row].col3);
                }
                throw new ArgumentOutOfRangeException("Invalid column type (" + typeof(T) + "), expected int or long");
            }

            public bool IsNull(int row, int column) => false;

            public int ColumnIndex(string columnName) => columns.IndexOf(columnName);

            public void Add(long sourceGroup, int sourceEntry, int sourceId, List<IConditionLine> conditions)
            {
                Span<long> columns = stackalloc long[3];
                this.conditions.Add(conditions);
                int i = 0;
                if (group != null)
                    columns[i++] = sourceGroup;
                if (entry != null)
                    columns[i++] = sourceEntry;
                if (id != null)
                    columns[i++] = sourceId;
                rows.Add((columns[0], (int)columns[1], (int)columns[2]));
            }

            public IList<IConditionLine>[] Conditions => conditions.ToArray();
        }
    }
}