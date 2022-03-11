using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Loaders
{
    [SingleInstance]
    [AutoRegister]
    public class DatabaseTableDataProvider : IDatabaseTableDataProvider
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly IMySqlExecutor sqlExecutor;
        private readonly IMessageBoxService messageBoxService;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IDatabaseTableModelGenerator tableModelGenerator;
        
        public DatabaseTableDataProvider(ITableDefinitionProvider tableDefinitionProvider, 
            IMySqlExecutor sqlExecutor,
            IMessageBoxService messageBoxService,
            IDatabaseProvider databaseProvider,
            IDatabaseTableModelGenerator tableModelGenerator)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.sqlExecutor = sqlExecutor;
            this.messageBoxService = messageBoxService;
            this.databaseProvider = databaseProvider;
            this.tableModelGenerator = tableModelGenerator;
        }
        
        private string BuildSQLQueryForSingleRow(DatabaseTableDefinitionJson tableDefinitionJson, string? customWhere, long? offset, int? limit)
        {
            var tableName = tableDefinitionJson.TableName;
            var columns = tableDefinitionJson.Groups
                .SelectMany(x => x.Fields)
                .Where(x => !x.IsConditionColumn && !x.IsMetaColumn)
                .Select(x => $"`{x.ForeignTable ?? tableName}`.`{x.DbColumnName}`")
                .Distinct();
            var names = string.Join(",", columns);
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
            return $"SELECT {names} FROM `{tableDefinitionJson.TableName}` {joins} {where} ORDER BY {string.Join(", ", tableDefinitionJson.PrimaryKey.Select(x => $"`{x}`"))} ASC LIMIT {limit ?? 300} OFFSET {offset ?? 0}";
        }
        
        private string BuildSQLQueryFromTableDefinition(DatabaseTableDefinitionJson tableDefinitionJson, DatabaseKey[] entries)
        {
            var tableName = tableDefinitionJson.TableName;
            var tablePrimaryKey = tableDefinitionJson.TablePrimaryKeyColumnName;
            var columns = tableDefinitionJson.Groups
                .SelectMany(x => x.Fields)
                .Where(x => !x.IsConditionColumn && !x.IsMetaColumn)
                .Select(x => $"`{x.ForeignTable ?? tableName}`.`{x.DbColumnName}`")
                .Distinct();
            var names = string.Join(",", columns);
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
            
            return
                $"SELECT {names} FROM {tableDefinitionJson.TableName} {joins} WHERE `{tableName}`.`{tablePrimaryKey}` IN ({string.Join(", ", entries)});";
        }
        
        public async Task<long> GetCount(string definitionId, string? customWhere, IEnumerable<DatabaseKey>? keys)
        {
            var definition = tableDefinitionProvider.GetDefinition(definitionId);
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
                var result = await sqlExecutor.ExecuteSelectSql(sql);
                return Convert.ToInt64(result[0]["num"].Item2);
            }
            catch (IMySqlExecutor.CannotConnectToDatabaseException)
            {
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
        
        public async Task<IDatabaseTableData?> Load(string definitionId, string? customWhere, long? offset, int? limit, DatabaseKey[]? keys)
        {
            var definition = tableDefinitionProvider.GetDefinition(definitionId);
            if (definition == null)
                return null;
            
            IList<Dictionary<string, (Type, object)>>? result = null;
            IDatabaseProvider.ConditionKeyMask keyMask = IDatabaseProvider.ConditionKeyMask.None;
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
                var sqlStatement = BuildSQLQueryForSingleRow(definition, customWhere, offset, limit);
                try
                {
                    result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

                    if (definition.Condition != null)
                    {
                        foreach (var row in result)
                        {
                            int? sourceGroup = null, sourceEntry = null, sourceId = null;

                            if (definition.Condition.SourceGroupColumn != null &&
                                row.TryGetValue(definition.Condition.SourceGroupColumn, out var groupData) &&
                                int.TryParse(groupData.Item2.ToString(), out var groupInt))
                                sourceGroup = groupInt;

                            if (definition.Condition.SourceEntryColumn != null &&
                                row.TryGetValue(definition.Condition.SourceEntryColumn, out var entryData) &&
                                int.TryParse(entryData.Item2.ToString(), out var entryInt))
                                sourceEntry = entryInt;

                            if (definition.Condition.SourceIdColumn != null &&
                                row.TryGetValue(definition.Condition.SourceIdColumn, out var idData) &&
                                int.TryParse(idData.Item2.ToString(), out var idInt))
                                sourceId = idInt;

                            IList<IConditionLine>? conditionList = await databaseProvider.GetConditionsForAsync(keyMask,
                                new IDatabaseProvider.ConditionKey(definition.Condition.SourceType, sourceGroup,
                                    sourceEntry, sourceId));
                            if (conditionList == null || conditionList.Count == 0)
                                conditionList = new List<IConditionLine>();
                            row.Add("conditions", (typeof(IList<IConditionLine>), conditionList!));
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
                if (definition.IsOnlyConditionsTable)
                {
                    if (definition.Condition == null)
                        throw new Exception("only_conditions + no conditions make no sense");
                    
                    result = new List<Dictionary<string, (Type, object)>>();
                    
                    foreach (var key in keys)
                    {
                        Debug.Assert(key.Count == 1, "todo?");
                        int? sourceGroup = null, sourceEntry = null, sourceId = null;
                        if (definition.Condition.SourceGroupColumn != null &&
                            definition.TablePrimaryKeyColumnName == definition.Condition.SourceGroupColumn)
                        {
                            keyMask = IDatabaseProvider.ConditionKeyMask.SourceGroup;
                            sourceGroup = (int)key[0];
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
                        
                        IList<IConditionLine>? conditionList = await databaseProvider.GetConditionsForAsync(keyMask,
                            new IDatabaseProvider.ConditionKey(definition.Condition.SourceType, sourceGroup,
                                sourceEntry, sourceId));
                        if (conditionList != null && conditionList.Count > 0)
                        {
                            foreach (var distinct in conditionList
                                .Select(line => (line.SourceEntry, line.SourceGroup, line.SourceId)).Distinct())
                            {
                                var row = new Dictionary<string, (Type, object)>();
                                var conditions = conditionList.Where(l =>
                                    l.SourceEntry == distinct.SourceEntry && l.SourceId == distinct.SourceId &&
                                                    l.SourceGroup == distinct.SourceGroup).ToList();
                                
                                foreach (var column in definition.TableColumns.Values)
                                {
                                    if (column.IsConditionColumn)
                                        continue;
                                    if (definition.Condition.SourceGroupColumn != null &&
                                        column.DbColumnName == definition.Condition.SourceGroupColumn)
                                        row.Add(column.DbColumnName, (typeof(int), distinct.SourceGroup));

                                    if (definition.Condition.SourceEntryColumn != null &&
                                        column.DbColumnName == definition.Condition.SourceEntryColumn)
                                        row.Add(column.DbColumnName, (typeof(int), distinct.SourceEntry));

                                    if (definition.Condition.SourceIdColumn != null &&
                                        column.DbColumnName == definition.Condition.SourceIdColumn)
                                        row.Add(column.DbColumnName, (typeof(int), distinct.SourceId));
                                }
                                row.Add("conditions", (typeof(IList<IConditionLine>), conditions));
                                result.Add(row);
                            } 
                        }
                    }
                }
                else
                {
                    Debug.Assert(customWhere == null, "Custom where with non single record mode is not supported");
                    var sqlStatement = BuildSQLQueryFromTableDefinition(definition, keys);
                    try
                    {
                        result = await sqlExecutor.ExecuteSelectSql(sqlStatement);

                        if (definition.Condition != null)
                        {
                            foreach (var row in result)
                            {
                                int? sourceGroup = null, sourceEntry = null, sourceId = null;

                                if (definition.Condition.SourceGroupColumn != null &&
                                    row.TryGetValue(definition.Condition.SourceGroupColumn, out var groupData) &&
                                    int.TryParse(groupData.Item2.ToString(), out var groupInt))
                                    sourceGroup = groupInt;

                                if (definition.Condition.SourceEntryColumn != null &&
                                    row.TryGetValue(definition.Condition.SourceEntryColumn, out var entryData) &&
                                    int.TryParse(entryData.Item2.ToString(), out var entryInt))
                                    sourceEntry = entryInt;

                                if (definition.Condition.SourceIdColumn != null &&
                                    row.TryGetValue(definition.Condition.SourceIdColumn, out var idData) &&
                                    int.TryParse(idData.Item2.ToString(), out var idInt))
                                    sourceId = idInt;

                                IList<IConditionLine>? conditionList = await databaseProvider.GetConditionsForAsync(keyMask,
                                    new IDatabaseProvider.ConditionKey(definition.Condition.SourceType, sourceGroup,
                                        sourceEntry, sourceId));
                                if (conditionList == null || conditionList.Count == 0)
                                    conditionList = new List<IConditionLine>();
                                row.Add("conditions", (typeof(IList<IConditionLine>), conditionList!));
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
                result = new List<Dictionary<string, (Type, object)>>();

            return tableModelGenerator.CreateDatabaseTable(definition, keys, result);
        }
    }
}