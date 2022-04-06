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
using WDE.Common.Services;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
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

    public DatabaseTablesFindAnywhereSource(IMySqlExecutor executor,
            ITableDefinitionProvider definitionProvider,
            Lazy<IDocumentManager> documentManager,
            IEventAggregator eventAggregator)
    {
        this.executor = executor;
        this.definitionProvider = definitionProvider;
        this.documentManager = documentManager;
        this.eventAggregator = eventAggregator;
    }
    
    public async Task Find(IFindAnywhereResultContext resultContext, IReadOnlyList<string> parameterName, long parameterValue, CancellationToken cancellationToken)
    {
        foreach (var definition in definitionProvider.Definitions)
        {
            if (definition.IsOnlyConditionsTable)
                continue;
            
            foreach (var tableGroup in definition.TableColumns.Values.GroupBy(c => c.ForeignTable ?? definition.TableName))
            {
                var tableName = tableGroup.Key;
                var table = Queries.Table(tableName);
                var where = table.ToWhere();

                if (tableName == definition.TableName && definition.Picker != null && parameterName.IndexOf(definition.Picker) != -1)
                {
                    where = where.OrWhere(row => row.Column<long>(definition.TablePrimaryKeyColumnName) == parameterValue);
                }
                
                foreach (var column in tableGroup)
                {
                    if (parameterName.IndexOf(column.ValueType) != -1)
                        where = where.OrWhere(row => row.Column<long>(column.DbColumnName) == parameterValue);
                }
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
                            item = new DatabaseTableSolutionItem(key, true, definition.Id, definition.IgnoreEquality);
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