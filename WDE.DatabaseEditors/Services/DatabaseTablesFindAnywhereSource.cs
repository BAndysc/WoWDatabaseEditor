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
    private readonly IMySqlExecutor executor;
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly Lazy<IDocumentManager> documentManager;
    private readonly IEventAggregator eventAggregator;
    private readonly IParameterFactory parameterFactory;

    public DatabaseTablesFindAnywhereSource(IMySqlExecutor executor,
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
    
    public async Task Find(IFindAnywhereResultContext resultContext, IReadOnlyList<string> parameterNames, long parameterValue, CancellationToken cancellationToken)
    {
        foreach (var definition in definitionProvider.Definitions)
        {
            if (definition.IsOnlyConditionsTable)
                continue;
            
            foreach (var tableGroup in definition.Groups.SelectMany(group => group.Fields.Select(column => (group, column))).GroupBy(c => c.column.ForeignTable ?? definition.TableName))
            {
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
                var result = await executor.ExecuteSelectSql(select.QueryString);

                if (result.Count > 0)
                {
                    foreach (var row in result)
                    {
                        ISolutionItem? item = null;
                        ICommand? commad = null;

                        if (definition.RecordMode == RecordMode.SingleRow)
                        {
                            var key = new DatabaseKey(definition.PrimaryKey.Select(k => Convert.ToInt64(row[k].Item2)));
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
                                    editor!.TryFind(key).ListenErrors();
                            });
                        }
                        else
                        {
                            DatabaseKey key;
                            if (tableName == definition.TableName)
                                key = new DatabaseKey(Convert.ToInt64(row[definition.TablePrimaryKeyColumnName].Item2));
                            else
                                key = new DatabaseKey(Convert.ToInt64(row[definition.ForeignTableByName![tableName].ForeignKeys[0]].Item2));
                            item = new DatabaseTableSolutionItem(key, true, false, definition.Id, definition.IgnoreEquality);
                        }
                        
                        resultContext.AddResult(new FindAnywhereResult(
                            new ImageUri(definition.IconPath!),
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