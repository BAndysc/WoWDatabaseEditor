using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
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

        private string BuildSQLQueryFromTableDefinition(in DatabaseTableDefinitionJson tableDefinitionJson, uint[] entries)
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
                    $"LEFT JOIN `{table.TableName}` ON `{table.TableName}`.`{table.ForeignKey}` = `{tableName}`.`{tablePrimaryKey}`"));
            }
            
            return
                $"SELECT {names} FROM {tableDefinitionJson.TableName} {joins} WHERE `{tableName}`.`{tablePrimaryKey}` IN ({string.Join(", ", entries)});";
        }

        public async Task<IDatabaseTableData?> Load(string definitionId, params uint[] keys)
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

            if (keys.Length > 0)
            {
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

                            var conditionList = await databaseProvider.GetConditionsForAsync(keyMask,
                                new IDatabaseProvider.ConditionKey(definition.Condition.SourceType, sourceGroup,
                                    sourceEntry, sourceId));
                            if (conditionList?.Count > 0)
                            {
                                row.Add("conditions", (typeof(IList<IConditionLine>), conditionList));
                            }
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

            if (result == null)
                result = new List<Dictionary<string, (Type, object)>>();

            return tableModelGenerator.CreateDatabaseTable(definition, keys, result);
        }
    }
}