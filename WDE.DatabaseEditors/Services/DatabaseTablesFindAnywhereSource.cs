using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Parameters;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.Services;

[AutoRegister]
[SingleInstance]
public class DatabaseTablesFindAnywhereSource : IFindAnywhereSource
{
    private readonly IDatabaseQueryExecutor executor;
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly Lazy<IDocumentManager> documentManager;
    private readonly IEventAggregator eventAggregator;
    private readonly IParameterFactory parameterFactory;

    public DatabaseTablesFindAnywhereSource(IDatabaseQueryExecutor executor,
            ITableDefinitionProvider definitionProvider,
            Lazy<IDocumentManager> documentManager,
            IEventAggregator eventAggregator,
            IParameterFactory parameterFactory)
    {
        this.executor = executor;
        this.definitionProvider = definitionProvider;
        this.documentManager = documentManager;
        this.eventAggregator = eventAggregator;
        this.parameterFactory = parameterFactory;
    }

    public FindAnywhereSourceType SourceType => FindAnywhereSourceType.Tables | FindAnywhereSourceType.Spawns;

    public async Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType searchType, IReadOnlyList<string> parameterNames, long parameterValue, CancellationToken cancellationToken)
    {
        foreach (var definition in definitionProvider.Definitions)
        {
            if (definition.IsOnlyConditionsTable)
                continue;

            if (!searchType.HasFlagFast(FindAnywhereSourceType.Spawns) &&
                definition.TableName is "creature" or "gameobject")
                continue;

            if (!searchType.HasFlagFast(FindAnywhereSourceType.Tables) &&
                definition.TableName is not "creature" and not "gameobject")
                continue;
            
            HashSet<DatabaseKey> added = new();
            foreach (var tableGroup in definition.Groups.SelectMany(group => group.Fields.Select(column => (group, column))).GroupBy(c => c.column.ForeignTable ?? definition.TableName))
            {
                added.Clear();
                var tableName = tableGroup.Key;
                var table = Queries.Table(tableName);
                var where = table.ToWhere();

                if (tableName == definition.TableName && definition.Picker != null && parameterNames.IndexOf(definition.Picker) != -1)
                {
                    where = where.OrWhere(row => row.Column<long>(definition.TablePrimaryKeyColumnName) == parameterValue);
                }
                
                foreach (var (group, column) in tableGroup)
                {
                    if (parameterNames.IndexOf(column.ValueType) != -1)
                    {
                        if (group.ShowIf is {} showIf)
                            where = where.OrWhere(row => row.Column<long>(column.DbColumnName) == parameterValue && row.Column<long>(showIf.ColumnName) == showIf.Value);
                        else
                            where = where.OrWhere(row => row.Column<long>(column.DbColumnName) == parameterValue);
                    }
                    else
                    {
                        var columnParameter = parameterFactory.Factory(column.ValueType);
                        if (columnParameter is DatabaseContextualParameter dependantParameter)
                        {
                            foreach (var parameterName in parameterNames)
                            {
                                var parameter = parameterFactory.Factory(parameterName);
                                var values = dependantParameter.DependantColumnValuesForParameter(parameter);
                                if (values != null && values.Count > 0)
                                {
                                    foreach (var value in values)
                                    {
                                        where = where.OrWhere(row =>
                                            row.Column<long>(dependantParameter.DependantColumn) == value &&
                                            row.Column<long>(column.DbColumnName) == parameterValue);
                                    }
                                }
                            }
                        }
                    }
                }

                if (where.IsEmpty)
                    continue;
                
                var select = where.Select();
                var result = await executor.ExecuteSelectSql(definition, select.QueryString);

                if (result.Count > 0)
                {
                    foreach (var row in result)
                    {
                        ISolutionItem? item = null;
                        ICommand? commad = null;

                        DatabaseKey key;
                        if (definition.RecordMode == RecordMode.SingleRow)
                        {
                            key = new DatabaseKey(definition.PrimaryKey.Select(k => Convert.ToInt64(row[k].Item2)));
                            commad = new DelegateCommand(() =>
                            {
                                var solutionItem = new DatabaseTableSolutionItem(definition.Id, definition.IgnoreEquality);
                                bool found = false;
                                SingleRowDbTableEditorViewModel? editor = null;
                                foreach (var doc in documentManager.Value.OpenedDocuments)
                                {
                                    if (doc is SingleRowDbTableEditorViewModel singleRowEditor &&
                                        singleRowEditor.TableDefinition.Id == definition.Id)
                                    {
                                        editor = singleRowEditor;
                                        found = true;
                                        documentManager.Value.ActiveDocument = editor;
                                    }
                                }

                                if (!found)
                                {
                                    eventAggregator.GetEvent<EventRequestOpenItem>().Publish(solutionItem);
                                    foreach (var doc in documentManager.Value.OpenedDocuments)
                                    {
                                        if (doc is SingleRowDbTableEditorViewModel singleRowEditor &&
                                            singleRowEditor.TableDefinition.Id == definition.Id)
                                        {
                                            editor = singleRowEditor;
                                            found = true;
                                        }
                                    }
                                }

                                if (found)
                                {
                                    async Task SelectRow()
                                    {
                                        if (definition.GroupByKey != null)
                                        {
                                            editor!.FilterViewModel.SelectedColumn = editor!.FilterViewModel.Columns.FirstOrDefault(c => c.ColumnName == definition.GroupByKey);
                                            editor!.FilterViewModel.SelectedOperator = editor!.FilterViewModel.Operators.First(o => o.Operator == "=");
                                            editor!.FilterViewModel.FilterText = row[definition.GroupByKey].Item2?.ToString() ?? "";
                                            await editor.FilterViewModel.ApplyFilter.ExecuteAsync();
                                        }
                                        await editor!.TryFind(key);
                                    }
                                    SelectRow().ListenErrors();
                                }
                            });
                        }
                        else
                        {
                            if (tableName == definition.TableName)
                                key = new DatabaseKey(Convert.ToInt64(row[definition.TablePrimaryKeyColumnName].Item2));
                            else
                                key = new DatabaseKey(Convert.ToInt64(row[definition.ForeignTableByName![tableName].ForeignKeys[0]].Item2));

                            if (!added.Add(key))
                                continue;
                            item = new DatabaseTableSolutionItem(key, true, false, definition.Id, definition.IgnoreEquality);
                        }
                        
                        resultContext.AddResult(new FindAnywhereResult(
                            new ImageUri(definition.IconPath!),
                            key[0],
                            tableName,
                            string.Join(", ", row.Select(pair => pair.Key + ": " + pair.Value.Item2)),
                            item,
                            commad
                            ));
                    }   
                }
            }

        }        
    }
}