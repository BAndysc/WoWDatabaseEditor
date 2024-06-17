using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Services.QueryParser;
using WDE.Common.Services.QueryParser.Models;
using WDE.Common.Sessions;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Solution;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[AutoRegister]
public class GenericTableQueryParserProvider : IQueryParserProvider
{
    private readonly ITableDefinitionProvider tableDefinitionProvider;
    private readonly IDatabaseTableDataProvider loader;
    private readonly ISessionService sessionService;

    private Dictionary<DatabaseTable, DatabaseTableSolutionItem> byTable = new();
    private Dictionary<DatabaseTable, List<DatabaseKey>> byTableToDelete = new();
    
    public GenericTableQueryParserProvider(ITableDefinitionProvider tableDefinitionProvider,
        IDatabaseTableDataProvider loader,
        ISessionService sessionService)
    {
        this.tableDefinitionProvider = tableDefinitionProvider;
        this.loader = loader;
        this.sessionService = sessionService;
    }

    private async Task<IList<DatabaseKey>> TryEvaluateKeys(DatabaseTableDefinitionJson definition,
        DatabaseForeignTableJson? foreign, 
        EqualityWhereCondition condition,
        IQueryParsingContext context)
    {
        List<DatabaseKey> keys = new();
        if (TryBuildKey(definition, foreign, condition, out var key))
        {
            keys.Add(key);
            return keys;
        }
        else if (definition.RecordMode == RecordMode.SingleRow)
        {
            var result = await loader.Load(definition.Id, condition.RawSql, 0, 100, null);
            if (result == null)
                return keys;
            if (result.Entities.Count == 100)
            {
                context.AddError($"Finding keys for table {definition.TableName} with condition '" + condition.RawSql + "' returned more than 100 rows, skpping");
                return keys;
            }
            keys.AddRange(result.Entities.Select(e => e.Key));
            return keys;
        }
        return keys;
    }

    private bool TryBuildKey(DatabaseTableDefinitionJson definition, DatabaseForeignTableJson? foreign, EqualityWhereCondition condition, out DatabaseKey key)
    {
        key = default;
        if (condition.Columns.Length != definition.GroupByKeys.Count ||
            definition.GroupByKeys.Count == 0)
            return false;

        List<long>? values = null;
        foreach (var groupBy in (foreign?.ForeignKeys ?? definition.GroupByKeys))
        {
            int index = condition.Columns.IndexIf(x => x.Equals(groupBy.ColumnName, StringComparison.InvariantCultureIgnoreCase));
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

    public async Task<bool> ParseDelete(DeleteQuery deleteQuery, IQueryParsingContext context)
    {
        var defi = tableDefinitionProvider.GetDefinitionByTableName(deleteQuery.TableName);
        if (defi == null)
            return false;

        if (defi.RecordMode == RecordMode.SingleRow)
        {
            foreach (var condition in deleteQuery.Where.Conditions)
            {
                var keys = await TryEvaluateKeys(defi, null, condition, context);
                foreach (var key in keys)
                {
                    if (key.Count != defi.GroupByKeys.Count)
                        continue;
                    var old = await loader.Load(defi.Id, null,null,null, new[]{key});
                    if (old != null && old.Entities.Count > 0)
                    {
                        if (!byTableToDelete.TryGetValue(defi.Id, out var toDelete))
                            toDelete = byTableToDelete[defi.Id] = new();

                        toDelete.Add(key);
                    }   
                }
            }
        }
        else if (defi.RecordMode == RecordMode.MultiRecord)
        {
            foreach (var condition in deleteQuery.Where.Conditions)
            {
                if (!TryBuildKey(defi, null, condition, out var key))
                    continue;
                if (key.Count != defi.GroupByKeys.Count)
                    continue;
                var item = new DatabaseTableSolutionItem(defi.Id, defi.IgnoreEquality);
                item.Entries.Add(new SolutionItemDatabaseEntity(key, false, false));
                context.ProduceItem(item);
            }
        }

        return true;
    }

    public async Task<bool> ParseInsert(InsertQuery query, IQueryParsingContext context)
    {
        var defi = tableDefinitionProvider.GetDefinitionByTableName(query.TableName);
        if (defi == null)
            return false;

        var keyIndices = defi.GroupByKeys.Select(key => query.Columns.IndexOf(key.ColumnName)).ToList();
        if (keyIndices.Count != defi.GroupByKeys.Count)
            return false;

        if (defi.RecordMode == RecordMode.SingleRow)
        {
            DatabaseTableSolutionItem? existing = null;
            if (!byTable.TryGetValue(defi.Id, out existing))
            {
                var phantom = new DatabaseTableSolutionItem(defi.Id, defi.IgnoreEquality);
                existing = (sessionService.CurrentSession?.Find(phantom) as DatabaseTableSolutionItem) ?? phantom;
                context.ProduceItem(existing);
                byTable[defi.Id] = existing;
            }
            foreach (var line in query.Inserts)
            {
                var key = new DatabaseKey(keyIndices.Select(index => line[index]).Select(x => (long)x));
                if (existing.Entries.All(x => x.Key != key))
                    existing.Entries.Add(new SolutionItemDatabaseEntity(key, false, false));
                if (byTableToDelete.TryGetValue(defi.Id, out var toDelete))
                    toDelete.Remove(key);
            }
        }
        else
        {
            Debug.Assert(keyIndices.Count == 1);
            foreach (var line in query.Inserts)
            {
                if (line[keyIndices[0]] is not long lkey)
                    continue;
                var item = new DatabaseTableSolutionItem(defi.Id, defi.IgnoreEquality);
                item.Entries.Add(new SolutionItemDatabaseEntity(new DatabaseKey(lkey), false, false));
                context.ProduceItem(item);
            }
        }

        return true;
    }

    public async Task<bool> ParseUpdate(UpdateQuery query, IQueryParsingContext context)
    {
        var defi = tableDefinitionProvider.GetDefinitionByTableName(query.TableName);
        DatabaseForeignTableJson? foreignTable = null;
        if (defi == null)
        {
            defi = tableDefinitionProvider.GetDefinitionByForeignTableName(query.TableName);
            if (defi?.ForeignTableByName?.TryGetValue(query.TableName.Table, out var foreignTable_) ?? false)
                foreignTable = foreignTable_;
        }

        if (defi == null)
            return false;
        
        foreach (var condition in query.Where.Conditions)
        {
            var keys = await TryEvaluateKeys(defi, null, condition, context);
            foreach (var key in keys)
            {
                var old = await loader.Load(defi.Id, null,null,null, new[]{key});
                if (old == null || old.Entities.Count != 1 || !old.Entities[0].ExistInDatabase)
                {
                    context.AddError($"{defi.TableName} where {defi.TablePrimaryKeyColumnName} = {key} not found, no update");
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
                        context.ProduceItem(existing);
                        byTable[defi.Id] = existing;
                    }

                    var entry = existing.Entries.FirstOrDefault(e => e.Key == key);
                    if (entry == null)
                    {
                        entry = new(key, true, false);
                        existing.Entries.Add(entry);
                    }
                    originals = entry.OriginalValues ??= new();
                }
                else
                {
                    var item = new DatabaseTableSolutionItem(defi.Id, defi.IgnoreEquality);
                    item.Entries.Add(new SolutionItemDatabaseEntity(key, true, false));
                    var savedItem = sessionService.Find(item);
                    if (savedItem != null)
                        item = (DatabaseTableSolutionItem)savedItem.Clone();

                    originals = item.Entries[0].OriginalValues ??= new();
                    context.ProduceItem(item);
                }
                var tableName = query.TableName.Table == defi.TableName ? null : query.TableName.Table;
                foreach (var upd in query.Updates)
                {
                    var cell = old.Entities[0].GetCell(new ColumnFullName(tableName, upd.ColumnName));
                    if (cell == null)
                        continue;
                    if (originals.Any(o => o.ColumnName == new ColumnFullName(tableName, upd.ColumnName)))
                        continue;
                    var original = new EntityOrigianlField()
                    {
                        ColumnName = new ColumnFullName(tableName, upd.ColumnName),
                        OriginalValue = cell.OriginalValue
                    };
                    originals.Add(original);
                }
            }
        }

        return true;
    }

    public void Finish(IQueryParsingContext context)
    {
        foreach (var toDelete in byTableToDelete)
        {
            DatabaseTableSolutionItem? existing = null;
            if (!byTable.TryGetValue(toDelete.Key, out existing))
            {
                var phantom = new DatabaseTableSolutionItem(toDelete.Key, true); // true, because we only have SingleRow here
                existing = (sessionService.CurrentSession?.Find(phantom) as DatabaseTableSolutionItem) ?? phantom;
                context.ProduceItem(existing);
            }
            existing.Entries.RemoveIf(e => toDelete.Value.Contains(e.Key));
            existing.DeletedEntries ??= new();
            existing.DeletedEntries.AddRange(toDelete.Value);
            existing.DeletedEntries = existing.DeletedEntries.Distinct().ToList();
        }
    }
}