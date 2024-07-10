using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Database.Counters;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.Services;

[AutoRegister]
[SingleInstance]
public class DatabaseRowsCountProvider : IDatabaseRowsCountProvider
{
    private readonly IMySqlExecutor executor;
    private readonly ITableDefinitionProvider definitionProvider;
    private Dictionary<DatabaseTable, BulkAsyncTableRowProvider> providers = new();

    public DatabaseRowsCountProvider(IMySqlExecutor executor, ITableDefinitionProvider definitionProvider)
    {
        this.executor = executor;
        this.definitionProvider = definitionProvider;
    }

    public async Task<int> GetRowsCountByPrimaryKey(DatabaseTable table, long primaryKey, CancellationToken token)
    {
        if (!providers.TryGetValue(table, out var provider))
        {
            var definition = definitionProvider.GetDefinition(table);
            if (definition == null)
                return 0;
            provider = new BulkAsyncTableRowProvider(executor, definition);
            providers[table] = provider;
        }

        return await provider.GetCount(primaryKey, token);
    }

    public async Task<int> GetCreaturesCountByEntry(long entry, CancellationToken token)
    {
        var table = DatabaseTable.WorldTable("creature_(by_entry)");
        if (!providers.TryGetValue(table, out var provider))
        {
            var definition = definitionProvider.GetDefinition(DatabaseTable.WorldTable("creature"));
            if (definition == null || (!definition.TableColumns.ContainsKey(new ColumnFullName(null, "id")) && !definition.TableColumns.ContainsKey(new ColumnFullName(null, "entry"))))
                return 0;
            var columnToGroupBy = definition.TableColumns.ContainsKey(new ColumnFullName(null, "entry")) ? "entry" : "id";
            provider = new BulkAsyncTableRowProvider(executor, definition, columnToGroupBy);
            providers[table] = provider;
        }

        return await provider.GetCount(entry, token);
    }

    public async Task<int> GetGameObjectCountByEntry(long entry, CancellationToken token)
    {
        var table = DatabaseTable.WorldTable("gameobject_(by_entry)");
        if (!providers.TryGetValue(table, out var provider))
        {
            var definition = definitionProvider.GetDefinition(DatabaseTable.WorldTable("gameobject"));
            if (definition == null || (!definition.TableColumns.ContainsKey(new ColumnFullName(null, "id")) && !definition.TableColumns.ContainsKey(new ColumnFullName(null, "entry"))))
                return 0;
            var columnToGroupBy = definition.TableColumns.ContainsKey(new ColumnFullName(null, "entry")) ? "entry" : "id";
            provider = new BulkAsyncTableRowProvider(executor, definition, columnToGroupBy);
            providers[table] = provider;
        }

        return await provider.GetCount(entry, token);
    }

    private class BulkAsyncTableRowProvider
    {
        private readonly IMySqlExecutor executor;
        private readonly DatabaseTableDefinitionJson tableDefinition;
        private Dictionary<long, TaskCompletionSource<int>> tasks = new();
        private Dictionary<long, List<CancellationToken>> requests = new();
        private ValuePublisher<long> valuePublisher = new ValuePublisher<long>();
        private string groupByColumn;

        public BulkAsyncTableRowProvider(IMySqlExecutor executor, DatabaseTableDefinitionJson tableDefinition, string? customGroupByColumn = null)
        {
            if (!tableDefinition.TablePrimaryKeyColumnName.HasValue || string.IsNullOrWhiteSpace(tableDefinition.TablePrimaryKeyColumnName.Value.ColumnName))
                throw new Exception($"Table {this.tableDefinition} primary key column name is not defined");
            
            this.executor = executor;
            this.tableDefinition = tableDefinition;
            groupByColumn = customGroupByColumn ?? tableDefinition.TablePrimaryKeyColumnName.Value.ColumnName;
            valuePublisher.Throttle(TimeSpan.FromSeconds((1)))
                .SubscribeAction(_ =>
                {
                    CalculateAsync().ListenErrors();
                });
        }

        private async Task CalculateAsync()
        {
            List<long> primaryKeysToFetch = new();
            List<long> cancelledPrimaryKeys = new();
            HashSet<long> done = new();
            foreach (var (primaryKey, completion) in tasks.ToList())
            {
                var cancellations = requests[primaryKey];
                var allCancelled = cancellations.All(c => c.IsCancellationRequested);
                
                if (!allCancelled)
                    primaryKeysToFetch.Add(primaryKey);
                
                cancelledPrimaryKeys.Add(primaryKey);
            }

            if (primaryKeysToFetch.Count == 0)
                return;

            var query = Queries.Table(tableDefinition.Id)
                .WhereIn(groupByColumn, primaryKeysToFetch)
                .SelectGroupBy(new[]{groupByColumn}, $"`{groupByColumn}`", "COUNT(*) AS c");
            
            var result = await executor.ExecuteSelectSql(query.QueryString);
            var groupByColumnIndex = result.ColumnIndex(groupByColumn);
            var cColumnIndex = result.ColumnIndex("c");
            foreach (var rowIndex in result)
            {
                var primaryKey = Convert.ToInt64(result.Value(rowIndex, groupByColumnIndex));
                var count = Convert.ToInt32(result.Value(rowIndex, cColumnIndex));
                done.Add(primaryKey);
                
                if (tasks.TryGetValue(primaryKey, out var task))
                {
                    task.SetResult(count);
                    tasks.Remove(primaryKey);
                    requests.Remove(primaryKey);
                }
            }

            foreach (var primaryKey in cancelledPrimaryKeys)
            {
                if (done.Contains(primaryKey))
                    continue;
                
                if (tasks.TryGetValue(primaryKey, out var task))
                {
                    task.SetResult(0);
                    tasks.Remove(primaryKey);
                    requests.Remove(primaryKey);
                } 
            }
        }

        public async Task<int> GetCount(long primaryKey, CancellationToken token)
        {
            if (tasks.TryGetValue(primaryKey, out var task))
            {
                requests[primaryKey].Add(token);
                return await task.Task;
            }

            var taskSource = new TaskCompletionSource<int>();
            tasks[primaryKey] = taskSource;
            requests[primaryKey] = new List<CancellationToken>(){token};
            valuePublisher.Publish(primaryKey);
            return await taskSource.Task;
        }
    }

}