using Prism.Events;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionSqlService : ISolutionSqlService
    {
        private readonly ISolutionItemSqlGeneratorRegistry sqlGeneratorRegistry;
        private readonly IEventAggregator eventAggregator;

        public SolutionSqlService(ISolutionItemSqlGeneratorRegistry sqlGeneratorRegistry, IEventAggregator eventAggregator)
        {
            this.sqlGeneratorRegistry = sqlGeneratorRegistry;
            this.eventAggregator = eventAggregator;
        }
        
        public async void OpenDocumentWithSqlFor(ISolutionItem? solutionItem)
        {
            if (solutionItem == null)
                return;
            MetaSolutionSQL solution = new(solutionItem);
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(solution);
        }
    }
}