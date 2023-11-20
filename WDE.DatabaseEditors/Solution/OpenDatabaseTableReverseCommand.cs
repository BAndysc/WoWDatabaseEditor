using System;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [SingleInstance]
    [ReverseRemoteCommand("open table")]
    [AutoRegister]
    public class OpenDatabaseTableReverseCommand : IReverseRemoteCommand
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly Lazy<IDocumentManager> documentManager;

        public OpenDatabaseTableReverseCommand(IEventAggregator eventAggregator,
            ITableDefinitionProvider tableDefinitionProvider,
            Lazy<IDocumentManager> documentManager)
        {
            this.eventAggregator = eventAggregator;
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.documentManager = documentManager;
        }
        
        public Task Invoke(ICommandArguments arguments)
        {
            if (!arguments.TryGetString(out var tableName))
                return Task.CompletedTask;

            var definition = tableDefinitionProvider.GetDefinition(DatabaseTable.WorldTable(tableName));
            if (definition == null)
                return Task.CompletedTask;

            if (definition.RecordMode == RecordMode.SingleRow)
            {
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(new DatabaseTableSolutionItem(definition.Id, definition.IgnoreEquality));
                
                if (!arguments.TryGetUint(out var entry_))
                    return Task.CompletedTask;
                
                foreach (var doc in documentManager.Value.OpenedDocuments)
                {
                    if (doc is SingleRowDbTableEditorViewModel singleRow && singleRow.TableDefinition.Id == definition.Id)
                    {
                        var rest = arguments.TakeRestArguments;
                        return singleRow.TryFind(new DatabaseKey(entry_), rest);
                    }
                }
                return Task.CompletedTask;
            }
            
            if (!arguments.TryGetUint(out var entry))
                return Task.CompletedTask;

            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(new DatabaseTableSolutionItem(new DatabaseKey(entry), true, false, DatabaseTable.WorldTable(tableName), definition.IgnoreEquality));
            return Task.CompletedTask;
        }

        public bool BringEditorToFront => true;
    }
}