using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Interfaces;
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

        public OpenDatabaseTableReverseCommand(IEventAggregator eventAggregator, ITableDefinitionProvider tableDefinitionProvider)
        {
            this.eventAggregator = eventAggregator;
            this.tableDefinitionProvider = tableDefinitionProvider;
        }
        
        public Task Invoke(ICommandArguments arguments)
        {
            if (!arguments.TryGetString(out var tableName))
                return Task.CompletedTask;
            
            if (!arguments.TryGetUint(out var entry))
                return Task.CompletedTask;

            var definition = tableDefinitionProvider.GetDefinition(tableName);
            if (definition == null)
                return Task.CompletedTask;
            
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(new DatabaseTableSolutionItem(entry, true, tableName, definition.IgnoreEquality));
            return Task.CompletedTask;
        }

        public bool BringEditorToFront => true;
    }
}