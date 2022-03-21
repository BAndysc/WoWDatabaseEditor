using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Services;
using WDE.Common.Sessions;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Solution;
using WDE.Module.Attributes;
using WDE.SqlInterpreter;
using WDE.SqlInterpreter.Models;

namespace WDE.DatabaseEditors.Services
{
    [AutoRegister]
    [SingleInstance]
    public class QueryParserService : IQueryParserService
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly IQueryEvaluator queryEvaluator;
        private readonly ISessionService sessionService;
        private readonly IDatabaseTableDataProvider loader;

        public QueryParserService(
            ITableDefinitionProvider tableDefinitionProvider,
            IQueryEvaluator queryEvaluator,
            ISessionService sessionService,
            IDatabaseTableDataProvider loader)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.queryEvaluator = queryEvaluator;
            this.sessionService = sessionService;
            this.loader = loader;
        }

        private bool TryBuildKey(DatabaseTableDefinitionJson definition, EqualityWhereCondition condition, out DatabaseKey key)
        {
            key = default;
            if (condition.Columns.Length != definition.GroupByKeys.Count ||
                definition.GroupByKeys.Count == 0)
                return false;

            List<long>? values = null;
            foreach (var groupBy in definition.GroupByKeys)
            {
                int index = condition.Columns.IndexIf(x => x.Equals(groupBy, StringComparison.InvariantCultureIgnoreCase));
                if (index == -1)
                    return false;
                var value = condition.Values[index];
                if (value is not long l)
                    return false;
                if (definition.GroupByKeys.Count == 1)
                {
                    key = new DatabaseKey(l);
                    return true;
                }
                values = values ?? new();
                values.Add(l);
            }

            key = new DatabaseKey(values!);
            return true;
        }
    
        public async Task<(IList<ISolutionItem> items, IList<string> errors)> GenerateItemsForQuery(string query)
        {
            IList<ISolutionItem> found = new List<ISolutionItem>();
            IList<string> errors = new List<string>();
            HashSet<string> missingTables = new HashSet<string>();
            Dictionary<string, DatabaseTableSolutionItem> byTable = new();
            Dictionary<string, List<DatabaseKey>> byTableToDelete = new();
            foreach (var q in queryEvaluator.Extract(query))
            {
                if (q is UpdateQuery updateQuery)
                {
                    var defi = tableDefinitionProvider.GetDefinitionByTableName(updateQuery.TableName);
                    DatabaseForeignTableJson? foreignTable = null;
                    if (defi == null)
                    {
                        defi = tableDefinitionProvider.GetDefinitionByForeignTableName(updateQuery.TableName);
                        if (defi?.ForeignTableByName?.TryGetValue(updateQuery.TableName, out var foreignTable_) ?? false)
                            foreignTable = foreignTable_;
                    }

                    if (defi == null)
                    {
                        missingTables.Add(updateQuery.TableName);
                        continue;
                    }
                    
                    foreach (var condition in updateQuery.Where.Conditions)
                    {
                        if (!TryBuildKey(defi, condition, out var key))
                            continue;
                        var old = await loader.Load(defi.Id, null,null,null, new[]{key});
                        if (old == null || old.Entities.Count != 1 || !old.Entities[0].ExistInDatabase)
                        {
                            errors.Add($"{defi.TableName} where {defi.TablePrimaryKeyColumnName} = {key} not found, no update");
                            continue;
                        }

                        List<EntityOrigianlField> originals;
                        if (defi.RecordMode == RecordMode.SingleRow)
                        {
                            DatabaseTableSolutionItem? existing = null;
                            if (!byTable.TryGetValue(defi.Id, out existing))
                            {
                                var phantom = new DatabaseTableSolutionItem(defi.Id, defi.IgnoreEquality);
                                existing = (sessionService.CurrentSession?.Find(phantom) as DatabaseTableSolutionItem) ?? phantom;
                                found.Add(existing);
                                byTable[defi.Id] = existing;
                            }

                            var entry = existing.Entries.FirstOrDefault(e => e.Key == key);
                            if (entry == null)
                            {
                                entry = new(key, true);
                                existing.Entries.Add(entry);
                            }
                            originals = entry.OriginalValues ??= new();
                        }
                        else
                        {
                            var item = new DatabaseTableSolutionItem(defi.Id, defi.IgnoreEquality);
                            item.Entries.Add(new SolutionItemDatabaseEntity(key, true));
                            var savedItem = sessionService.Find(item);
                            if (savedItem != null)
                                item = (DatabaseTableSolutionItem)savedItem.Clone();

                            originals = item.Entries[0].OriginalValues ??= new();
                            found.Add(item);
                        }
                        foreach (var upd in updateQuery.Updates)
                        {
                            var cell = old.Entities[0].GetCell(upd.ColumnName);
                            if (cell == null)
                                continue;
                            if (originals.Any(o => o.ColumnName == upd.ColumnName))
                                continue;
                            var original = new EntityOrigianlField()
                            {
                                ColumnName = upd.ColumnName,
                                OriginalValue = cell.OriginalValue
                            };
                            originals.Add(original);
                        }
                    }
                }
                else if (q is DeleteQuery deleteQuery)
                {
                    var defi = tableDefinitionProvider.GetDefinitionByTableName(deleteQuery.TableName);
                    if (defi == null)
                    {
                        missingTables.Add(deleteQuery.TableName);
                        continue;
                    }

                    if (defi.RecordMode != RecordMode.SingleRow)
                        continue;
                    
                    foreach (var condition in deleteQuery.Where.Conditions)
                    {
                        if (!TryBuildKey(defi, condition, out var key))
                            continue;
                        if (key.Count != defi.GroupByKeys.Count)
                            continue;
                        var old = await loader.Load(defi.Id, null,null,null, new[]{key});
                        if (old == null || old.Entities.Count == 0)
                        {
                            if (!byTableToDelete.TryGetValue(defi.Id, out var toDelete))
                                toDelete = byTableToDelete[defi.Id] = new();

                            toDelete.Add(key);
                        }
                    }
                }
                else if (q is InsertQuery insertQuery)
                {
                    var defi = tableDefinitionProvider.GetDefinitionByTableName(insertQuery.TableName);
                    if (defi == null)
                    {
                        missingTables.Add(insertQuery.TableName);
                        continue;
                    }

                    var keyIndices = defi.GroupByKeys.Select(key => insertQuery.Columns.IndexOf(key)).ToList();
                    if (keyIndices.Count != defi.GroupByKeys.Count)
                    {
                        missingTables.Add(insertQuery.TableName);
                        continue;
                    }

                    if (defi.RecordMode == RecordMode.SingleRow)
                    {
                        DatabaseTableSolutionItem? existing = null;
                        if (!byTable.TryGetValue(defi.Id, out existing))
                        {
                            var phantom = new DatabaseTableSolutionItem(defi.Id, defi.IgnoreEquality);
                            existing = (sessionService.CurrentSession?.Find(phantom) as DatabaseTableSolutionItem) ?? phantom;
                            found.Add(existing);
                            byTable[defi.Id] = existing;
                        }
                        foreach (var line in insertQuery.Inserts)
                        {
                            var key = new DatabaseKey(keyIndices.Select(index => line[index]).Select(x => (long)x));
                            if (existing.Entries.All(x => x.Key != key))
                                existing.Entries.Add(new SolutionItemDatabaseEntity(key, false));
                            if (byTableToDelete.TryGetValue(defi.Id, out var toDelete))
                                toDelete.Remove(key);
                        }
                    }
                    else
                    {
                        Debug.Assert(keyIndices.Count == 1);
                        foreach (var line in insertQuery.Inserts)
                        {
                            if (line[keyIndices[0]] is not long lkey)
                                continue;
                            var item = new DatabaseTableSolutionItem(defi.Id, defi.IgnoreEquality);
                            item.Entries.Add(new SolutionItemDatabaseEntity(new DatabaseKey(lkey), false));
                            found.Add(item);
                        }
                    }
                }
            }

            foreach (var toDelete in byTableToDelete)
            {
                DatabaseTableSolutionItem? existing = null;
                if (!byTable.TryGetValue(toDelete.Key, out existing))
                {
                    var phantom = new DatabaseTableSolutionItem(toDelete.Key, false); // false, because we only have SingleRow here
                    existing = (sessionService.CurrentSession?.Find(phantom) as DatabaseTableSolutionItem) ?? phantom;
                    found.Add(existing);
                }
                existing.Entries.RemoveIf(e => toDelete.Value.Contains(e.Key));
                existing.DeletedEntries ??= new();
                existing.DeletedEntries.AddRange(toDelete.Value);
                existing.DeletedEntries = existing.DeletedEntries.Distinct().ToList();
            }
            
            foreach (var missing in missingTables)
                errors.Add($"Table `{missing}` is not supported in WDE, no item added to the session.");
        
            return (found, errors);
        }
    }
}