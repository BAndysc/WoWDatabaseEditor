using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Services;
using WDE.Common.Sessions;
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
    
        public async Task<(IList<ISolutionItem> items, IList<string> errors)> GenerateItemsForQuery(string query)
        {
            IList<ISolutionItem> found = new List<ISolutionItem>();
            IList<string> errors = new List<string>();
            HashSet<string> missingTables = new HashSet<string>();
            foreach (var q in queryEvaluator.Extract(query))
            {
                if (q is UpdateQuery updateQuery)
                {
                    var defi = tableDefinitionProvider.GetDefinitionByTableName(updateQuery.TableName);
                    DatabaseForeignTableJson? foreignTable = null;
                    if (defi == null)
                    {
                        defi = tableDefinitionProvider.GetDefinitionByForeignTableName(updateQuery.TableName);
                        if (defi?.ForeignTableByName.TryGetValue(updateQuery.TableName, out var foreignTable_) ?? false)
                            foreignTable = foreignTable_;
                    }

                    if (defi == null)
                    {
                        missingTables.Add(updateQuery.TableName);
                        continue;
                    }
                
                    if (
                        (!foreignTable.HasValue && !updateQuery.Where.ColumnName.Equals(defi.TablePrimaryKeyColumnName, StringComparison.InvariantCultureIgnoreCase))
                        ||
                        (foreignTable.HasValue && !updateQuery.Where.ColumnName.Equals(foreignTable.Value.ForeignKeys[0], StringComparison.InvariantCultureIgnoreCase))
                        )
                    {
                        missingTables.Add(updateQuery.TableName);
                        continue;
                    }

                    foreach (var key in updateQuery.Where.Values)
                    {
                        if (key is not long lkey)
                            continue;
                        var old = await loader.Load(defi.Id, (uint)lkey);
                        if (old == null || old.Entities.Count != 1 || !old.Entities[0].ExistInDatabase)
                        {
                            errors.Add($"{defi.TableName} where {defi.TablePrimaryKeyColumnName} = {lkey} not found, no update");
                            continue;
                        }
                        var item = new DatabaseTableSolutionItem(defi.Id);
                        item.Entries.Add(new SolutionItemDatabaseEntity((uint)lkey, true));
                        var savedItem = sessionService.Find(item);
                        if (savedItem != null)
                            item = (DatabaseTableSolutionItem)savedItem.Clone();

                        var originals = item.Entries[0].OriginalValues ??= new();
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
                        found.Add(item);
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
                
                    int indexOf = -1;
                    for (int i = 0; i < insertQuery.Columns.Count; ++i)
                    {
                        if (insertQuery.Columns[i].Equals(defi.TablePrimaryKeyColumnName,
                            StringComparison.InvariantCultureIgnoreCase))
                        {
                            indexOf = i;
                            break;
                        }
                    }

                    if (indexOf == -1)
                    {
                        missingTables.Add(insertQuery.TableName);
                        continue;
                    }

                    foreach (var line in insertQuery.Inserts)
                    {
                        if (line[indexOf] is not long lkey)
                            continue;
                        var item = new DatabaseTableSolutionItem(defi.Id);
                        item.Entries.Add(new SolutionItemDatabaseEntity((uint)lkey, false));
                        found.Add(item);
                    }
                }
            }
            foreach (var missing in missingTables)
                errors.Add($"Table `{missing}` is not supported in WDE, no item added to the session.");
        
            return (found, errors);
        }
    }
}