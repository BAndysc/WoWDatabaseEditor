using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [SingleInstance]
    [ReverseRemoteCommand("open table")]
    [AutoRegister]
    public class OpenDatabaseTableReverseCommand : IReverseRemoteCommand
    {
        private readonly IEventAggregator eventAggregator;

        public OpenDatabaseTableReverseCommand(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }
        
        public Task Invoke(ICommandArguments arguments)
        {
            if (!arguments.TryGetString(out var tableName))
                return Task.CompletedTask;
            
            if (!arguments.TryGetUint(out var entry))
                return Task.CompletedTask;
            
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(new DatabaseTableSolutionItem(entry, true, tableName));
            return Task.CompletedTask;
        }

        public bool BringEditorToFront => true;
    }
}